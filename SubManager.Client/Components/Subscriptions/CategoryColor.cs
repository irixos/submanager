using MudBlazor.Utilities;

namespace SubManager.Client.Components.Subscriptions;

internal static class CategoryColor
{
    private const string Fallback = "#757575";

    public static string Normalize(string? color) =>
        color is { Length: 7 } && color[0] == '#' ? color : Fallback;

    public static string GetForeground(string? color)
    {
        var parsed = new MudColor(Normalize(color));
        var luminance = (parsed.R * 299 + parsed.G * 587 + parsed.B * 114) / 1000;
        return luminance >= 150 ? "#1A1A1A" : "#FFFFFF";
    }
}
