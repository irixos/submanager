using Microsoft.JSInterop;

namespace SubManager.Client.Pages;

public sealed class SubscriptionsInterop(IJSRuntime js) : IAsyncDisposable
{
    private const string ModulePath = "./Pages/Subscriptions.razor.js";
    private const string GetPreferencesMethod = "getPreferences";
    private const string SetShowCategoriesMethod = "setShowCategories";
    private const string SetGridViewMethod = "setGridView";
    private IJSObjectReference? module;

    private async ValueTask<IJSObjectReference> GetModuleAsync()
    {
        return module ??= await js.InvokeAsync<IJSObjectReference>("import", ModulePath);
    }

    public async ValueTask<SubscriptionsPreferences> GetPreferencesAsync()
    {
        var jsModule = await GetModuleAsync();
        return await jsModule.InvokeAsync<SubscriptionsPreferences>(GetPreferencesMethod);
    }

    public async ValueTask SetShowCategoriesAsync(bool value)
    {
        var jsModule = await GetModuleAsync();
        await jsModule.InvokeVoidAsync(SetShowCategoriesMethod, value);
    }

    public async ValueTask SetGridViewAsync(bool value)
    {
        var jsModule = await GetModuleAsync();
        await jsModule.InvokeVoidAsync(SetGridViewMethod, value);
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

public sealed class SubscriptionsPreferences
{
    public bool ShowCategories { get; init; }

    public bool GridView { get; init; }
}
