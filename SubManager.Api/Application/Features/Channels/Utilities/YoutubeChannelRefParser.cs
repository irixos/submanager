using System.Text;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using SubManager.Api.Application.Features.Channels.Models;
using static SubManager.Api.Application.Features.YouTubeUrlPatterns;

namespace SubManager.Api.Application.Features.Channels.Utilities;

public static class YoutubeChannelRefParser
{
    private static readonly char[] TrimChars = ['<', '>', '(', ')', '"', '\''];

    private static readonly (Regex Regex, YoutubeChannelRefKind Kind)[] YoutubeUrlRegexes =
    [
        (YoutubeChannelIdRegex, YoutubeChannelRefKind.Id),
        (YouTubeHandleUrlRegex, YoutubeChannelRefKind.Handle),
        (YouTubeCustomUrlRegex, YoutubeChannelRefKind.Custom),
        (YouTubeLegacyUserUrlRegex, YoutubeChannelRefKind.Username)
    ];

    public static YoutubeChannelRef Parse(string channelUrl)
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

    public static async Task<IReadOnlyCollection<YoutubeChannelRef>> ParseFile(IFormFile file, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(file);

        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync(ct);

        var matches = YoutubeChannelIdFileRegex.Matches(content);
        if (matches.IsNullOrEmpty()) return [];

        return matches.Select(m => new YoutubeChannelRef(
            YoutubeChannelRefKind.Id,
            NormalizeYouTubeUrl(m.Groups["canonical"].Value).ToString()))
            .ToList();
    }
    
    public static string GetChannelId(YoutubeChannelRef channelRef)
    {
        if (channelRef.Kind != YoutubeChannelRefKind.Id)
            throw new ArgumentException("Channel ref must be an ID ref.", nameof(channelRef));

        var uri = NormalizeYouTubeUrl(channelRef.Url);

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments is not ["channel", var channelId])
            throw new ArgumentException("Channel ref URL must be a canonical channel URL.", nameof(channelRef));

        return channelId;
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
