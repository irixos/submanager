using System.Text.RegularExpressions;

namespace SubManager.Api.Application.Features;

internal static class YouTubeUrlPatterns
{
    public const string SupportedYoutubeDomainPattern =
        @"(?:youtube\.com|www\.youtube\.com|m\.youtube\.com)";

    public static readonly Regex YouTubeHandleUrlRegex = new(
        $@"^(?<canonical>(?:https?://)?{SupportedYoutubeDomainPattern}/@[^/?#\s]+)(?:[/?#].*)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static readonly Regex YoutubeChannelIdRegex = new(
        $@"^(?<canonical>(?:https?://)?{SupportedYoutubeDomainPattern}/channel/UC[0-9A-Za-z_-]{{22}})(?:[/?#].*)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static readonly Regex YouTubeLegacyUserUrlRegex = new(
        $@"^(?<canonical>(?:https?://)?{SupportedYoutubeDomainPattern}/user/[^/?#\s]+)(?:[/?#].*)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static readonly Regex YouTubeCustomUrlRegex = new(
        $@"^(?<canonical>(?:https?://)?{SupportedYoutubeDomainPattern}/c/[^/?#\s]+)(?:[/?#].*)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static readonly Regex YoutubeChannelIdFileRegex = new(
        $@"(?<canonical>(?:https?://)?{SupportedYoutubeDomainPattern}/channel/UC[0-9A-Za-z_-]{{22}})(?:[/?#][^\s""'<]*)?",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static readonly Regex YouTubeShortUrlRegex = new(
        $@"^(?:https?://)?{SupportedYoutubeDomainPattern}/shorts/[0-9A-Za-z_-]{{11}}(?:[/?#].*)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
}