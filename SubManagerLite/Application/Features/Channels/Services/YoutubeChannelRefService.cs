using System.Text;
using System.Text.RegularExpressions;
using SubManagerLite.Application.Features.Channels.Interfaces;
using SubManagerLite.Application.Features.Channels.Models;

namespace SubManagerLite.Application.Features.Channels.Services;

public sealed class YoutubeChannelRefService() : IYoutubeChannelRefService
{
    private static readonly char[] TrimChars = ['<', '>', '(', ')', '"', '\''];

    private const string SupportedYoutubeDomainPattern =
        @"(?:youtube\.com|www\.youtube\.com|m\.youtube\.com)";

    private static readonly Regex YouTubeHandleUrlRegex = new(
        $@"^(?<canonical>(?:https?://)?{SupportedYoutubeDomainPattern}/@[^/?#\s]+)(?:[/?#].*)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex YoutubeChannelIdRegex = new(
        $@"^(?<canonical>(?:https?://)?{SupportedYoutubeDomainPattern}/channel/UC[0-9A-Za-z_-]{{22}})(?:[/?#].*)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex YouTubeLegacyUserUrlRegex = new(
        $@"^(?<canonical>(?:https?://)?{SupportedYoutubeDomainPattern}/user/[^/?#\s]+)(?:[/?#].*)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex YouTubeCustomUrlRegex = new(
        $@"^(?<canonical>(?:https?://)?{SupportedYoutubeDomainPattern}/c/[^/?#\s]+)(?:[/?#].*)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly (Regex Regex, YoutubeChannelRefKind Kind)[] YoutubeUrlRegexes =
    [
        (YoutubeChannelIdRegex, YoutubeChannelRefKind.Id),
        (YouTubeHandleUrlRegex, YoutubeChannelRefKind.Handle),
        (YouTubeCustomUrlRegex, YoutubeChannelRefKind.Custom),
        (YouTubeLegacyUserUrlRegex, YoutubeChannelRefKind.Username)
    ];

    public YoutubeChannelRef GetYoutubeChannelRef(string channelUrl)
    {
        var trimmed = SanitizeUrl(channelUrl);
        var uri = NormalizeYouTubeUrl(trimmed);
        var normalizedUrl = uri.ToString();

        foreach (var (regex, kind) in YoutubeUrlRegexes)
        {
            var match = regex.Match(normalizedUrl);
            if (!match.Success) continue;

            var canonicalUrl = NormalizeYouTubeUrl(match.Groups["canonical"].Value).ToString();
            return new YoutubeChannelRef(kind, canonicalUrl);
        }

        throw new ArgumentException(
            "The provided URL is not a valid YouTube channel URL.",
            nameof(channelUrl)
        );
    }

    private static string SanitizeUrl(string channelUrl)
    {
        if (string.IsNullOrWhiteSpace(channelUrl))
        {
            throw new ArgumentException("Channel URL cannot be null or empty.", nameof(channelUrl));
        }

        var trimmed = channelUrl.Trim().Trim(TrimChars).Trim().Normalize(NormalizationForm.FormC);

        return trimmed;
    }

    private static Uri NormalizeYouTubeUrl(string input)
    {
        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            // Assume https when the user pastes a bare host/path (common UI input).
            if (!Uri.TryCreate($"https://{input}", UriKind.Absolute, out uri))
            {
                throw new ArgumentException(
                    "Channel URL must be a valid absolute URL (with or without a scheme).",
                    nameof(input));
            }
        }

        return uri;
    }
}
