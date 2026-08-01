using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using MudBlazor;
using SubManager.ApiClient;
using SubManager.Client.Components.Feed;

namespace SubManager.Client.Pages;

public partial class Feed
{
    private const int PageSize = 20;
    private const int FilterPageSize = 100;
    private const int DurationStatusBatchSize = 100;
    private const int MaximumDurationPollFailures = 5;
    private static readonly TimeSpan DurationPollInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan MaximumDurationRetryDelay = TimeSpan.FromSeconds(10);
    private readonly List<VideoResponse> videos = [];
    private readonly List<CategoryResponse> categories = [];
    private readonly List<ChannelResponse> channels = [];
    private readonly HashSet<int> watchedUpdates = [];
    private readonly Dictionary<int, DateTimeOffset?> watchedDateOverrides = [];
    private readonly CancellationTokenSource lifetimeCancellationTokenSource = new();
    private CancellationTokenSource loadCancellationTokenSource = new();
    private CancellationTokenSource durationPollingCancellationTokenSource = new();
    private Task durationPollingTask = Task.CompletedTask;
    private IReadOnlyCollection<int> selectedCategoryIds = [];
    private IReadOnlyCollection<int> selectedChannelIds = [];
    private string searchText = string.Empty;
    private bool isGrid = true;
    private FeedInterop? interop;
    private DotNetObjectReference<Feed>? dotNetReference;
    private ElementReference pullRefreshElement;
    private int currentPage = 1;
    private bool isLoading;
    private bool hasLoaded;
    private bool hasMoreVideos = true;
    private bool isRefreshing;
    private bool showCategories;
    private bool durationMetadataActive;
    private bool initialized;
    private Exception? loadError;
    private Exception? filterOptionsError;
    private int? appliedInitialChannelId;

    [Parameter, SupplyParameterFromQuery(Name = "channel")]
    public int? InitialChannelId { get; set; }

    private bool HasFilters =>
        !string.IsNullOrWhiteSpace(searchText) ||
        selectedCategoryIds.Count > 0 ||
        selectedChannelIds.Count > 0;

    protected override async Task OnInitializedAsync()
    {
        ApplyInitialChannelFilter();
        await LoadFilterOptions();
        await FetchVideos();
        initialized = true;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!initialized || InitialChannelId == appliedInitialChannelId)
            return;

        ApplyInitialChannelFilter();
        await ReloadVideos();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        interop = new FeedInterop(JS);
        var storedViewMode = await interop.GetViewModeAsync();
        if (storedViewMode is not null)
            isGrid = !string.Equals(storedViewMode, "list", StringComparison.OrdinalIgnoreCase);

        showCategories = await interop.GetShowCategoriesAsync();
        StateHasChanged();

        dotNetReference = DotNetObjectReference.Create(this);
        await interop.InitializePullToRefreshAsync(pullRefreshElement, dotNetReference);
    }

    private async Task LoadFilterOptions()
    {
        filterOptionsError = null;

        try
        {
            var loadedCategories = await LoadAllCategories();
            var loadedChannels = await LoadAllChannels();

            categories.Clear();
            categories.AddRange(loadedCategories);
            channels.Clear();
            channels.AddRange(loadedChannels);
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException || !lifetimeCancellationTokenSource.IsCancellationRequested)
        {
            Logger.LogError(exception, "Unable to load feed filter options");
            filterOptionsError = exception;
        }
    }

    private void ApplyInitialChannelFilter()
    {
        appliedInitialChannelId = InitialChannelId;
        selectedChannelIds = InitialChannelId.HasValue ? [InitialChannelId.Value] : [];
    }

    private async Task<List<CategoryResponse>> LoadAllCategories()
    {
        var result = new List<CategoryResponse>();
        var page = 1;

        while (true)
        {
            var response = await CategoriesClient.GetCategoriesAsync(
                page, FilterPageSize, "Name, Id", null, lifetimeCancellationTokenSource.Token);
            var items = response.Data?.ToList() ?? [];
            result.AddRange(items);

            if (items.Count < FilterPageSize ||
                response.Count.HasValue && result.Count >= response.Count.Value)
                return result;

            page++;
        }
    }

    private async Task<List<ChannelResponse>> LoadAllChannels()
    {
        var result = new List<ChannelResponse>();
        var page = 1;

        while (true)
        {
            var response = await ChannelsClient.GetChannelsAsync(
                page, FilterPageSize, "Name, Id", null, lifetimeCancellationTokenSource.Token);
            var items = response.Data?.ToList() ?? [];
            result.AddRange(items);

            if (items.Count < FilterPageSize ||
                response.Count.HasValue && result.Count >= response.Count.Value)
                return result;

            page++;
        }
    }

    private async Task FetchVideos()
    {
        if (isLoading || !hasMoreVideos)
            return;

        var cancellationToken = loadCancellationTokenSource.Token;
        isLoading = true;
        loadError = null;

        try
        {
            var videoPage = await VideosClient.GetVideosAsync(
                currentPage, PageSize, "PublishedDate desc, Id desc", BuildFilter(), cancellationToken);

            if (cancellationToken != loadCancellationTokenSource.Token)
                return;

            var newVideos = videoPage.Data?.ToList() ?? [];
            foreach (var video in newVideos)
            {
                if (video.Id is int id && watchedDateOverrides.TryGetValue(id, out var watchedDate))
                    SetWatchedDate(video, watchedDate);
            }

            videos.AddRange(newVideos);
            currentPage++;
            EnsureDurationPolling();

            hasMoreVideos = newVideos.Count > 0 &&
                (videoPage.Count.HasValue
                    ? videos.Count < videoPage.Count.Value
                    : newVideos.Count == PageSize);
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            Logger.LogError(exception, "Unable to load feed page {Page}", currentPage);
            loadError = exception;
        }
        finally
        {
            if (cancellationToken == loadCancellationTokenSource.Token)
            {
                isLoading = false;
                hasLoaded = true;
            }
        }
    }

    private string BuildFilter()
    {
        var filters = new List<string> { "IsShort=false" };
        if (!string.IsNullOrWhiteSpace(searchText))
            filters.Add($"Title=*{EscapeGridifyValue(searchText.Trim())}/i");

        filters.AddRange(selectedCategoryIds.Select(id => $"Categories.Id={id}"));

        if (selectedChannelIds.Count == 1)
            filters.Add($"Channel.Id={selectedChannelIds.First()}");
        else if (selectedChannelIds.Count > 1)
            filters.Add($"({string.Join('|', selectedChannelIds.Select(id => $"Channel.Id={id}"))})");

        return string.Join(',', filters);
    }

    private static string EscapeGridifyValue(string value)
    {
        return Regex.Replace(value, "([(),|\\\\])", "\\$1")
            .Replace("/i", "\\/i", StringComparison.OrdinalIgnoreCase);
    }

    private async Task PersistViewMode()
    {
        if (interop is not null)
            await interop.SetViewModeAsync(isGrid ? "grid" : "list");
    }

    private async Task PersistShowCategories()
    {
        if (interop is not null)
            await interop.SetShowCategoriesAsync(showCategories);
    }

    private async Task ClearFilters()
    {
        selectedCategoryIds = [];
        selectedChannelIds = [];
        searchText = string.Empty;
        await ReloadVideos();
    }

    private async Task ReloadVideos()
    {
        await ResetDurationPolling();
        await loadCancellationTokenSource.CancelAsync();
        loadCancellationTokenSource.Dispose();
        loadCancellationTokenSource = new CancellationTokenSource();

        videos.Clear();
        currentPage = 1;
        hasMoreVideos = true;
        hasLoaded = false;
        isLoading = false;
        await FetchVideos();
    }

    private void EnsureDurationPolling()
    {
        if (!durationPollingTask.IsCompleted ||
            videos.All(video => video.DurationSeconds.HasValue || !video.Id.HasValue))
            return;

        durationMetadataActive = true;
        durationPollingTask = PollDurations(durationPollingCancellationTokenSource.Token);
    }

    private async Task PollDurations(CancellationToken ct)
    {
        var failureCount = 0;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var missingIds = videos
                    .Where(video => video.DurationSeconds is null && video.Id.HasValue)
                    .Select(video => video.Id!.Value)
                    .ToArray();
                var hasPendingMetadata = false;
                var durationsUpdated = false;

                foreach (var ids in missingIds.Chunk(DurationStatusBatchSize))
                {
                    var status = await VideosClient.GetVideoDurationStatusAsync(ids, ct);
                    hasPendingMetadata |= status.HasPendingMetadata is true;

                    foreach (var duration in status.Videos ?? [])
                    {
                        var video = videos.FirstOrDefault(video =>
                            video.Id == duration.Id &&
                            video.DurationSeconds is null);
                        if (video is null || !duration.DurationSeconds.HasValue)
                            continue;

                        video.DurationSeconds = duration.DurationSeconds;
                        durationsUpdated = true;
                    }
                }

                failureCount = 0;
                var stillMissing = videos.Any(video =>
                    video.DurationSeconds is null && video.Id.HasValue);

                if (!stillMissing || !hasPendingMetadata)
                {
                    durationMetadataActive = false;
                    StateHasChanged();
                    return;
                }

                if (durationsUpdated)
                    StateHasChanged();

                await Task.Delay(DurationPollInterval, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, "Unable to poll video duration metadata");
                failureCount++;
                if (failureCount >= MaximumDurationPollFailures)
                {
                    durationMetadataActive = false;
                    StateHasChanged();
                    return;
                }

                var retryDelay = TimeSpan.FromMilliseconds(Math.Min(
                    MaximumDurationRetryDelay.TotalMilliseconds,
                    DurationPollInterval.TotalMilliseconds * Math.Pow(2, failureCount)));

                try
                {
                    await Task.Delay(retryDelay, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
            }
        }
    }

    private async Task ResetDurationPolling()
    {
        await durationPollingCancellationTokenSource.CancelAsync();
        await durationPollingTask;
        durationPollingCancellationTokenSource.Dispose();
        durationPollingCancellationTokenSource = new CancellationTokenSource();
        durationPollingTask = Task.CompletedTask;
        durationMetadataActive = false;
    }

    private Task MarkWatched(VideoResponse video) =>
        video.IsWatched is true
            ? Task.CompletedTask
            : UpdateWatchedDate(video, DateTimeOffset.UtcNow);

    private Task RestoreUnwatched(VideoResponse video) =>
        video.IsWatched is not true
            ? Task.CompletedTask
            : UpdateWatchedDate(video, null);

    private async Task UpdateWatchedDate(
        VideoResponse video,
        DateTimeOffset? watchedDate)
    {
        if (video.Id is not int id || !watchedUpdates.Add(id))
            return;

        var previousWatchedDate = video.WatchedDate;
        watchedDateOverrides[id] = watchedDate;
        SetWatchedDate(video, watchedDate);

        try
        {
            await VideosClient.UpdateVideoWatchedDateAsync(
                id,
                new UpdateVideoWatchedDateRequest { WatchedDate = watchedDate },
                lifetimeCancellationTokenSource.Token);
            ApplyWatchedDateOverride(id);
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException || !lifetimeCancellationTokenSource.IsCancellationRequested)
        {
            Logger.LogError(exception, "Unable to update watched state for video {VideoId}", video.Id);
            watchedDateOverrides[id] = previousWatchedDate;
            ApplyWatchedDateOverride(id);
            Snackbar.Add("Unable to update the watched status. The previous state was restored.", Severity.Error);
        }
        finally
        {
            watchedUpdates.Remove(id);
        }
    }

    private void ApplyWatchedDateOverride(int id)
    {
        var video = videos.FirstOrDefault(video => video.Id == id);
        if (video is not null)
            SetWatchedDate(video, watchedDateOverrides[id]);
    }

    private static void SetWatchedDate(VideoResponse video, DateTimeOffset? watchedDate)
    {
        video.IsWatched = watchedDate.HasValue;
        video.WatchedDate = watchedDate;
    }

    private async Task RefreshFeed()
    {
        if (isRefreshing)
            return;

        isRefreshing = true;
        try
        {
            await VideosClient.RefreshVideosAsync(lifetimeCancellationTokenSource.Token);
            await ReloadVideos();
            Snackbar.Add("Feed refreshed.", Severity.Success, options =>
            {
                options.VisibleStateDuration = 5000;
                options.HideTransitionDuration = 1000;
                options.CloseButtonClickFunc = snackbar =>
                {
                    snackbar.ForceClose();
                    return Task.CompletedTask;
                };
            });
        }
        catch (ApiException exception) when (exception.StatusCode == 409)
        {
            Snackbar.Add("A feed refresh is already running.", Severity.Warning);
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException || !lifetimeCancellationTokenSource.IsCancellationRequested)
        {
            Logger.LogError(exception, "Unable to refresh feed");
            Snackbar.Add("Unable to refresh the feed. Please try again.", Severity.Error);
        }
        finally
        {
            isRefreshing = false;
        }
    }

    [JSInvokable]
    public async Task OnPullToRefresh()
    {
        await InvokeAsync(RefreshFeed);
    }

    public async ValueTask DisposeAsync()
    {
        await lifetimeCancellationTokenSource.CancelAsync();
        await loadCancellationTokenSource.CancelAsync();
        await durationPollingCancellationTokenSource.CancelAsync();
        await durationPollingTask;

        if (interop is not null)
            await interop.DisposeAsync();

        dotNetReference?.Dispose();
        lifetimeCancellationTokenSource.Dispose();
        loadCancellationTokenSource.Dispose();
        durationPollingCancellationTokenSource.Dispose();
    }
}
