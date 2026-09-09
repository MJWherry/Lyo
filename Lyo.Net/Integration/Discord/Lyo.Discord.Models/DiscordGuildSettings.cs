using System.Diagnostics;

namespace Lyo.Discord.Models;

/// <summary>Per-guild bot settings stored in the config store, keyed by <c>EntityRef.For&lt;DiscordGuild&gt;(guildId)</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class DiscordGuildSettings
{
    /// <summary>Persisted schema version; bump when adding migrations in <see cref="NormalizeForRead" />.</summary>
    public const int CurrentSchemaVersion = 3;

    /// <summary>Forward-compatible evolution of this document schema version.</summary>
    public int Version { get; set; } = CurrentSchemaVersion;

    /// <summary>Channel that accepts bot commands; null = no restriction by channel.</summary>
    public ulong? CommandChannelId { get; set; }

    /// <summary>Channel for guild-scoped errors and operational notices; null = disabled.</summary>
    public ulong? LogChannelId { get; set; }

    /// <summary>Bot permission checks role treated as server admin.</summary>
    public ulong? AdminRoleId { get; set; }

    /// <summary>Bot permission checks role treated as moderator.</summary>
    public ulong? ModRoleId { get; set; }

    /// <summary>If provided, <c>/comic read</c> and chapter-reader buttons are restricted to this channel.</summary>
    public ulong? ComicReaderChannelId { get; set; }

    /// <summary>BCP-47 language filter applied to comic search and chapter lists in this guild. Present only when supplied.</summary>
    public string? ComicDefaultLanguage { get; set; }

    /// <summary>If false (default), comic reading is rejected in NSFW channels.</summary>
    public bool ComicNsfwAllowed { get; set; }

    /// <summary>
    /// Monotonic binding revision of the stored value (latest snapshot). Filled when loaded from the config store API; cleared before writes so it is not saved inside
    /// the binding JSON.
    /// </summary>
    public int? Revision { get; set; }

    /// <summary>Upgrade stored JSON older than <see cref="CurrentSchemaVersion" /> (e.g. missing <see cref="Version" />).</summary>
    public void NormalizeForRead()
    {
        if (Version <= 0)
            Version = CurrentSchemaVersion;

        if (Version == 1)
            Version = 2;

        if (Version == 2)
            Version = 3;
    }

    /// <summary>Invoke before persist so stored documents always carry the current schema version.</summary>
    public void NormalizeForPersistence()
    {
        NormalizeForRead();
        Version = CurrentSchemaVersion;
    }

    public override string ToString() => $"DiscordGuildSettings: v{Version}, commandChannel={CommandChannelId}, logChannel={LogChannelId}";
}