using System.Text;
using Gridify;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SubManager.Api.Application.Entities;
using SubManager.Api.Application.Features.Channels.Models;
using SubManager.Api.Application.Features.Channels.Services;
using SubManager.Api.Application.Features.Videos.Models;
using SubManager.Api.Application.Interfaces;
using SubManager.Api.Tests.Infrastructure;
using Xunit;

namespace SubManager.Api.Tests.Application.Features.Channels.Services;

public sealed class ChannelServiceTests
{
    [Fact]
    public async Task GetAllAndGetAsync_ExistingChannels_ReturnNestedProjections()
    {
        using var database = new SqliteTestDatabase();
        var category = new Category { Name = "Technology", Color = "#123456" };
        var channel = CreateChannel("UC-list", "Listed");
        channel.Categories.Add(category);
        database.Context.Add(channel);
        await database.Context.SaveChangesAsync();
        var service = CreateService(database, _ => ChannelInfo("unused"));

        var page = await service.GetAllAsync(
            new GridifyQuery { Page = 1, PageSize = 10 },
            CancellationToken.None);
        var found = await service.GetAsync(channel.Id, CancellationToken.None);
        var missing = await service.GetAsync(channel.Id + 1, CancellationToken.None);

        Assert.Equal(1, page.Count);
        var item = Assert.Single(page.Data);
        Assert.Equal("Listed", item.Name);
        Assert.Equal("Technology", Assert.Single(item.Categories).Name);
        Assert.Equal(channel.Id, found?.Id);
        Assert.Null(missing);
    }

    [Fact]
    public async Task CreateAsync_MetadataAndCategories_PersistsMappedChannel()
    {
        using var database = new SqliteTestDatabase();
        var category = new Category { Name = "Science" };
        database.Context.Add(category);
        await database.Context.SaveChangesAsync();
        YoutubeChannelRef? requestedRef = null;
        var service = CreateService(database, channelRef =>
        {
            requestedRef = channelRef;
            return ChannelInfo("UC-created", "Created", "https://image");
        });

        var result = await service.CreateAsync(
            new CreateChannelRequest
            {
                ChannelUrl = "youtube.com/@created",
                CategoryIds = [category.Id]
            },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(YoutubeChannelRefKind.Handle, requestedRef?.Kind);
        Assert.Equal("UC-created", result.YoutubeChannelId);
        Assert.Equal("Created", result.Name);
        Assert.Equal("https://image", result.ThumbnailUrl);
        Assert.True(result.IsActive);
        Assert.Equal("Science", Assert.Single(result.Categories).Name);
        Assert.Equal(1, await database.Context.Channels.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_DuplicateYoutubeId_ReturnsNull()
    {
        using var database = new SqliteTestDatabase();
        database.Context.Add(CreateChannel("UC-duplicate", "Existing"));
        await database.Context.SaveChangesAsync();
        var service = CreateService(database, _ => ChannelInfo("UC-duplicate", "Duplicate"));

        var result = await service.CreateAsync(
            new CreateChannelRequest { ChannelUrl = "youtube.com/@duplicate" },
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(1, await database.Context.Channels.CountAsync());
    }

    [Fact]
    public async Task ImportAsync_MixedCandidates_ReturnsExactCountsAndPersistsNewChannel()
    {
        using var database = new SqliteTestDatabase();
        var existingId = "UC" + new string('1', 22);
        database.Context.Add(CreateChannel(existingId, "Existing"));
        await database.Context.SaveChangesAsync();
        var service = CreateService(database, channelRef =>
        {
            if (channelRef.Url.Contains("failure", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Unavailable");
            if (channelRef.Url.Contains("existing", StringComparison.OrdinalIgnoreCase))
                return ChannelInfo(existingId, "Existing");
            if (channelRef.Url.Contains("bravo", StringComparison.OrdinalIgnoreCase))
                return ChannelInfo("UC-alpha", "Same resolved channel");
            return ChannelInfo("UC-alpha", "Alpha");
        });
        var file = FormFile(
            """
            https://youtube.com/@alpha
            https://youtube.com/@ALPHA
            https://youtube.com/user/bravo
            https://youtube.com/@existing
            https://youtube.com/@failure
            """);

        var result = await service.ImportAsync(
            new ImportChannelsRequest { File = file },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.CandidatesFound);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(3, result.DuplicateCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal("UC-alpha", Assert.Single(result.ImportedChannels).YoutubeChannelId);
        Assert.Equal(2, await database.Context.Channels.CountAsync());
    }

    [Fact]
    public async Task ImportAsync_NoUrls_ReturnsNullWithoutCallingProvider()
    {
        using var database = new SqliteTestDatabase();
        var calls = 0;
        var service = CreateService(database, _ =>
        {
            calls++;
            return ChannelInfo("unused");
        });

        var result = await service.ImportAsync(
            new ImportChannelsRequest { File = FormFile("not a channel URL") },
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task ImportAsync_ProviderCancellation_Propagates()
    {
        using var database = new SqliteTestDatabase();
        var service = CreateService(
            database,
            _ => throw new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.ImportAsync(
                new ImportChannelsRequest { File = FormFile("https://youtube.com/@cancel") },
                CancellationToken.None));
    }

    [Fact]
    public async Task UpdateCategoriesAsync_ReplaceClearAndInvalidIds_BehaveAtomically()
    {
        using var database = new SqliteTestDatabase();
        var first = new Category { Name = "First" };
        var second = new Category { Name = "Second" };
        var channel = CreateChannel("UC-categories", "Categories");
        channel.Categories.Add(first);
        database.Context.AddRange(channel, second);
        await database.Context.SaveChangesAsync();
        var service = CreateService(database, _ => ChannelInfo("unused"));

        var replaced = await service.UpdateCategoriesAsync(
            channel.Id,
            new UpdateChannelCategoriesRequest { CategoryIds = [second.Id, second.Id] },
            CancellationToken.None);
        Assert.True(replaced);
        Assert.Equal("Second", Assert.Single(channel.Categories).Name);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateCategoriesAsync(
                channel.Id,
                new UpdateChannelCategoriesRequest { CategoryIds = [first.Id, 999] },
                CancellationToken.None));
        Assert.Equal("Second", Assert.Single(channel.Categories).Name);

        var cleared = await service.UpdateCategoriesAsync(
            channel.Id,
            new UpdateChannelCategoriesRequest { CategoryIds = null },
            CancellationToken.None);
        Assert.True(cleared);
        Assert.Empty(channel.Categories);
        Assert.False(await service.UpdateCategoriesAsync(
            channel.Id + 1,
            new UpdateChannelCategoriesRequest(),
            CancellationToken.None));
    }

    [Fact]
    public async Task StatusAndDelete_FoundAndMissing_ReturnExpectedState()
    {
        using var database = new SqliteTestDatabase();
        var channel = CreateChannel("UC-state", "State");
        database.Context.Add(channel);
        await database.Context.SaveChangesAsync();
        var service = CreateService(database, _ => ChannelInfo("unused"));

        Assert.True(await service.UpdateStatusAsync(
            channel.Id,
            new UpdateChannelStatusRequest { IsActive = false },
            CancellationToken.None));
        Assert.False(channel.IsActive);
        Assert.False(await service.UpdateStatusAsync(
            channel.Id + 1,
            new UpdateChannelStatusRequest(),
            CancellationToken.None));
        Assert.False(await service.DeleteAsync(channel.Id + 1, CancellationToken.None));
        Assert.True(await service.DeleteAsync(channel.Id, CancellationToken.None));
        Assert.Empty(await database.Context.Channels.ToListAsync());
    }

    private static ChannelService CreateService(
        SqliteTestDatabase database,
        Func<YoutubeChannelRef, YoutubeChannelInfo> getChannelInfo)
    {
        return new ChannelService(
            database.Context,
            new StubMetadataProvider(getChannelInfo));
    }

    private static Channel CreateChannel(string youtubeId, string name)
    {
        return new Channel
        {
            YoutubeChannelId = youtubeId,
            Name = name,
            AddedDate = DateTimeOffset.UtcNow,
            IsActive = true
        };
    }

    private static YoutubeChannelInfo ChannelInfo(
        string id,
        string name = "Channel",
        string? thumbnail = null)
    {
        return new YoutubeChannelInfo
        {
            YoutubeChannelId = id,
            Name = name,
            ThumbnailUrl = thumbnail
        };
    }

    private static FormFile FormFile(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "channels.txt");
    }

    private sealed class StubMetadataProvider(
        Func<YoutubeChannelRef, YoutubeChannelInfo> getChannelInfo) : IYoutubeMetadataProvider
    {
        public Task<YoutubeChannelInfo> GetChannelInfo(
            YoutubeChannelRef youtubeChannelRef,
            CancellationToken ct) => Task.FromResult(getChannelInfo(youtubeChannelRef));

        public Task<YoutubeVideoInfo> GetVideoInfo(
            string videoId,
            CancellationToken ct) => throw new NotSupportedException();
    }
}
