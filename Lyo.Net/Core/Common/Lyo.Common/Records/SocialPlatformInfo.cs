using System.Diagnostics;
using System.Reflection;
using Lyo.Common.Extensions;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Lyo.Common.Records;

/// <summary>Represents metadata about a social media platform, using <see cref="Slug" /> as the canonical identifier.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SocialPlatformInfo(
    string Name,
    string Slug,
    string Description,
    string? WebsiteUri,
    string? ProfileUriTemplate,
    bool IsFederated,
    string[] Aliases)
{
    public static readonly SocialPlatformInfo Unknown = new(
        "Unknown", "unknown", "Unknown or unspecified social platform", null, null, false, ["unknown", "unspecified"]);

    public static readonly SocialPlatformInfo Other = new(
        "Other", "other", "Other social platform not listed in the registry", null, null, false, []);

    public static readonly SocialPlatformInfo LinkedIn = new(
        "LinkedIn", "linkedin", "Professional networking platform", "https://www.linkedin.com", "https://www.linkedin.com/in/{username}", false, []);

    public static readonly SocialPlatformInfo X = new(
        "X", "x", "Microblogging and social networking platform (formerly Twitter)", "https://x.com", "https://x.com/{username}", false,
        ["twitter"]);

    public static readonly SocialPlatformInfo Facebook = new(
        "Facebook", "facebook", "Social networking platform", "https://www.facebook.com", "https://www.facebook.com/{username}", false, ["fb"]);

    public static readonly SocialPlatformInfo Instagram = new(
        "Instagram", "instagram", "Photo and video sharing platform", "https://www.instagram.com", "https://www.instagram.com/{username}", false, ["ig"]);

    public static readonly SocialPlatformInfo Threads = new(
        "Threads", "threads", "Text-based conversation app by Meta", "https://www.threads.net", "https://www.threads.net/@{username}", false, []);

    public static readonly SocialPlatformInfo Bluesky = new(
        "Bluesky", "bluesky", "Decentralized social network", "https://bsky.app", "https://bsky.app/profile/{username}", false, ["bsky"]);

    public static readonly SocialPlatformInfo Mastodon = new(
        "Mastodon", "mastodon", "Federated microblogging platform (ActivityPub)", "https://joinmastodon.org", "https://{instance}/@{username}", true, []);

    public static readonly SocialPlatformInfo TikTok = new(
        "TikTok", "tiktok", "Short-form video platform", "https://www.tiktok.com", "https://www.tiktok.com/@{username}", false, []);

    public static readonly SocialPlatformInfo YouTube = new(
        "YouTube", "youtube", "Video sharing platform", "https://www.youtube.com", "https://www.youtube.com/@{username}", false, ["yt"]);

    public static readonly SocialPlatformInfo Snapchat = new(
        "Snapchat", "snapchat", "Multimedia messaging app", "https://www.snapchat.com", "https://www.snapchat.com/add/{username}", false, []);

    public static readonly SocialPlatformInfo Pinterest = new(
        "Pinterest", "pinterest", "Visual discovery and bookmarking platform", "https://www.pinterest.com", "https://www.pinterest.com/{username}", false, []);

    public static readonly SocialPlatformInfo Reddit = new(
        "Reddit", "reddit", "Community discussion and content aggregation platform", "https://www.reddit.com", "https://www.reddit.com/user/{username}", false, []);

    public static readonly SocialPlatformInfo Discord = new(
        "Discord", "discord", "Voice, video, and text communication platform", "https://discord.com", null, false, []);

    public static readonly SocialPlatformInfo GitHub = new(
        "GitHub", "github", "Software development and version control platform", "https://github.com", "https://github.com/{username}", false, []);

    public static readonly SocialPlatformInfo GitLab = new(
        "GitLab", "gitlab", "DevOps and source code management platform", "https://gitlab.com", "https://gitlab.com/{username}", false, []);

    public static readonly SocialPlatformInfo StackOverflow = new(
        "Stack Overflow", "stackoverflow", "Question and answer site for developers", "https://stackoverflow.com", "https://stackoverflow.com/users/{username}", false,
        ["stack-overflow"]);

    public static readonly SocialPlatformInfo Medium = new(
        "Medium", "medium", "Online publishing and blogging platform", "https://medium.com", "https://medium.com/@{username}", false, []);

    public static readonly SocialPlatformInfo Substack = new(
        "Substack", "substack", "Newsletter publishing platform", "https://substack.com", "https://{username}.substack.com", false, []);

    public static readonly SocialPlatformInfo Twitch = new(
        "Twitch", "twitch", "Live streaming platform", "https://www.twitch.tv", "https://www.twitch.tv/{username}", false, []);

    public static readonly SocialPlatformInfo Behance = new(
        "Behance", "behance", "Creative portfolio platform by Adobe", "https://www.behance.net", "https://www.behance.net/{username}", false, []);

    public static readonly SocialPlatformInfo Dribbble = new(
        "Dribbble", "dribbble", "Design portfolio and community platform", "https://dribbble.com", "https://dribbble.com/{username}", false, []);

    public static readonly SocialPlatformInfo Vimeo = new(
        "Vimeo", "vimeo", "Video hosting and sharing platform", "https://vimeo.com", "https://vimeo.com/{username}", false, []);

    private static readonly Dictionary<string, SocialPlatformInfo> BySlug = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, SocialPlatformInfo> ByName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, SocialPlatformInfo> ByAlias = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<SocialPlatformInfo> AllPlatforms = [];

    /// <summary>Gets all registered social platforms except <see cref="Unknown" />.</summary>
    public static IReadOnlyList<SocialPlatformInfo> All => AllPlatforms;

    static SocialPlatformInfo()
    {
        var fields = typeof(SocialPlatformInfo).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(SocialPlatformInfo))
            .Select(f => (SocialPlatformInfo)f.GetValue(null)!)
            .ToList();

        foreach (var platform in fields) {
            if (platform == Unknown)
                continue;

            AllPlatforms.Add(platform);
            BySlug[Normalize(platform.Slug)] = platform;
            ByName[Normalize(platform.Name)] = platform;
            ByAlias[Normalize(platform.Slug)] = platform;
            ByAlias[Normalize(platform.Name)] = platform;

            foreach (var alias in platform.Aliases.Where(a => !a.IsNullOrWhitespace()))
                ByAlias[Normalize(alias)] = platform;
        }
    }

    public override string ToString() => $"{Name} ({Slug})";

    /// <summary>Finds a platform by its canonical slug.</summary>
    public static SocialPlatformInfo FromSlug(string? slug)
    {
        if (slug.IsNullOrWhitespace())
            return Unknown;

        return BySlug.TryGetValue(Normalize(slug), out var platform) ? platform : Unknown;
    }

    /// <summary>Finds a platform by its display name.</summary>
    public static SocialPlatformInfo FromName(string? name)
    {
        if (name.IsNullOrWhitespace())
            return Unknown;

        return ByName.TryGetValue(Normalize(name), out var platform) ? platform : FromAlias(name);
    }

    /// <summary>Finds a platform by slug, display name, or alias.</summary>
    public static SocialPlatformInfo FromAlias(string? alias)
    {
        if (alias.IsNullOrWhitespace())
            return Unknown;

        return ByAlias.TryGetValue(Normalize(alias), out var platform) ? platform : Unknown;
    }

    /// <summary>Builds a profile URL from a username or ActivityPub-style handle when a template is available.</summary>
    public string? TryBuildProfileUri(string? username)
    {
        if (ProfileUriTemplate.IsNullOrWhitespace() || username.IsNullOrWhitespace() || this == Unknown || this == Other)
            return null;

        var handle = NormalizeHandle(username);
        if (handle.IsNullOrWhitespace())
            return null;

        if (IsFederated)
            return TryBuildFederatedProfileUri(handle);

        return ProfileUriTemplate!.Replace("{username}", handle);
    }

    private string? TryBuildFederatedProfileUri(string handle)
    {
        var atIndex = handle.IndexOf('@');
        if (atIndex <= 0 || atIndex >= handle.Length - 1)
            return null;

        var localUser = handle.Substring(0, atIndex);
        var instance = handle.Substring(atIndex + 1);
        if (localUser.IsNullOrWhitespace() || instance.IsNullOrWhitespace())
            return null;

        return ProfileUriTemplate!
            .Replace("{username}", localUser)
            .Replace("{instance}", instance);
    }

    private static string NormalizeHandle(string username)
    {
        var trimmed = username.Trim();
        return trimmed.StartsWith("@", StringComparison.Ordinal) ? trimmed.Substring(1).Trim() : trimmed;
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}
