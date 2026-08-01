using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SubManager.Client.Components.Shared;

internal sealed class PageToolbarInterop(IJSRuntime js) : IAsyncDisposable
{
    private const string ModulePath = "./Components/Shared/PageToolbar.razor.js";
    private const string InitializeMethod = "initialize";
    private const string DisposeMethod = "dispose";
    private IJSObjectReference? module;
    private IJSObjectReference? toolbar;

    public async ValueTask InitializeAsync(ElementReference element)
    {
        module = await js.InvokeAsync<IJSObjectReference>("import", ModulePath);
        toolbar = await module.InvokeAsync<IJSObjectReference>(InitializeMethod, element);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (toolbar is not null)
            {
                await toolbar.InvokeVoidAsync(DisposeMethod);
                await toolbar.DisposeAsync();
            }

            if (module is not null)
                await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
