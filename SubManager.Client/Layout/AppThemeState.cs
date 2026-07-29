using MudBlazor;

namespace SubManager.Client.Layout;

public sealed class AppThemeState
{
    public bool IsDarkMode { get; private set; } = true;

    public MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Black = "#110e2d",
            AppbarText = "#424242",
            AppbarBackground = "#ffffff",
            DrawerBackground = "#ffffff",
            GrayLight = "#e8e8e8",
            GrayLighter = "#f9f9f9",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#7e6fff",
            Surface = "#1e1e2d",
            Background = "#1a1a27",
            BackgroundGray = "#151521",
            AppbarText = "#92929f",
            AppbarBackground = "#1a1a27",
            DrawerBackground = "#1a1a27",
            ActionDefault = "#74718e",
            ActionDisabled = "#9999994d",
            ActionDisabledBackground = "#605f6d4d",
            TextPrimary = "#b2b0bf",
            TextSecondary = "#92929f",
            TextDisabled = "#ffffff33",
            DrawerIcon = "#92929f",
            DrawerText = "#92929f",
            GrayLight = "#2a2833",
            GrayLighter = "#1e1e2d",
            Info = "#4a86ff",
            Success = "#3dcb6c",
            Warning = "#ffb545",
            Error = "#ff3f5f",
            LinesDefault = "#33323e",
            TableLines = "#33323e",
            Divider = "#292838",
            OverlayLight = "#1e1e2d80",
        },
        LayoutProperties = new LayoutProperties()
    };

    public string Icon => IsDarkMode
        ? Icons.Material.Rounded.AutoMode
        : Icons.Material.Outlined.DarkMode;

    public void Toggle() => IsDarkMode = !IsDarkMode;
}
