namespace SubManager.Client.Components.Subscriptions;

internal static class CategoryColor
{
    private const string Fallback = "#757575";

    public static string Normalize(string? color)
    {
        return color is { Length: 7 } && color[0] == '#'
            ? color
            : Fallback;
    }

    public static string GetForeground(string? color)
    {
        var normalized = Normalize(color);
        var red = Convert.ToInt32(normalized.Substring(1, 2), 16);
        var green = Convert.ToInt32(normalized.Substring(3, 2), 16);
        var blue = Convert.ToInt32(normalized.Substring(5, 2), 16);
        var luminance = (red * 299 + green * 587 + blue * 114) / 1000;
        return luminance >= 150 ? "#1A1A1A" : "#FFFFFF";
    }
}
