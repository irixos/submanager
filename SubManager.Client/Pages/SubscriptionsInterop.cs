using Microsoft.JSInterop;

namespace SubManager.Client.Pages;

public sealed class SubscriptionsInterop(IJSRuntime js) : IAsyncDisposable
{
    private const string ModulePath = "./Pages/Subscriptions.razor.js";
    private const string GetShowCategoriesMethod = "getShowCategories";
    private const string SetShowCategoriesMethod = "setShowCategories";
    private IJSObjectReference? module;

    private async ValueTask<IJSObjectReference> GetModuleAsync()
    {
        return module ??= await js.InvokeAsync<IJSObjectReference>("import", ModulePath);
    }

    public async ValueTask<bool> GetShowCategoriesAsync()
    {
        var jsModule = await GetModuleAsync();
        return await jsModule.InvokeAsync<bool>(GetShowCategoriesMethod);
    }

    public async ValueTask SetShowCategoriesAsync(bool value)
    {
        var jsModule = await GetModuleAsync();
        await jsModule.InvokeVoidAsync(SetShowCategoriesMethod, value);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (module is not null)
                await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
