using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SubManager.Client.Pages;

public sealed class FeedInterop(IJSRuntime js) : IAsyncDisposable
{
    private const string ModulePath = "./Pages/Feed.razor.js";
    private const string GetViewModeMethod = "getViewMode";
    private const string SetViewModeMethod = "setViewMode";
    private const string InitializePullToRefreshMethod = "initializePullToRefresh";
    private const string DisposeMethod = "dispose";
    private IJSObjectReference? module;

    private async ValueTask<IJSObjectReference> GetModuleAsync()
    {
        return module ??= await js.InvokeAsync<IJSObjectReference>("import", ModulePath);
    }

    public async ValueTask<string?> GetViewModeAsync()
    {
        var jsModule = await GetModuleAsync();
        return await jsModule.InvokeAsync<string?>(GetViewModeMethod);
    }

    public async ValueTask SetViewModeAsync(string value)
    {
        var jsModule = await GetModuleAsync();
        await jsModule.InvokeVoidAsync(SetViewModeMethod, value);
    }

    public async ValueTask InitializePullToRefreshAsync(
        ElementReference element,
        DotNetObjectReference<Feed> dotNetRef)
    {
        var jsModule = await GetModuleAsync();
        await jsModule.InvokeVoidAsync(InitializePullToRefreshMethod, element, dotNetRef);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (module is not null)
            {
                await module.InvokeVoidAsync(DisposeMethod);
                await module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
