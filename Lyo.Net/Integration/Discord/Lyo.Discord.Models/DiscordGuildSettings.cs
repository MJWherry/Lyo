using System.Diagnostics;

namespace Lyo.Discord.Models;

/// <summary>Per-guild bot configuration stored in the config store, keyed by <c>EntityRef.For&lt;DiscordGuild&gt;(guildId)</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class DiscordGuildSettings
{
    /// <summary>Current persisted schema version; bump when adding migrations in <see cref="NormalizeForRead" />.</summary>
    public const int CurrentSchemaVersion = 3;

    /// <summary>Schema version for forward-compatible evolution of this document.</summary>
    public int Version { get; set; } = CurrentSchemaVersion;

    /// <summary>Channel where bot commands are accepted; null = no restriction by channel.</summary>
    public ulong? CommandChannelId { get; set; }

    /// <summary>Channel where the bot posts guild-scoped errors and operational notices; null = disabled.</summary>
    public ulong? LogChannelId { get; set; }

    /// <summary>Role treated as server admin for bot permission checks.</summary>
    public ulong? AdminRoleId { get; set; }

    /// <summary>Role treated as moderator for bot permission checks.</summary>
    public ulong? ModRoleId { get; set; }

    /// <summary>When set, <c>/comic read</c> and chapter-reader buttons are restricted to this channel.</summary>
    public ulong? ComicReaderChannelId { get; set; }

    /// <summary>Optional BCP-47 language filter applied to comic search and chapter lists in this guild.</summary>
    public string? ComicDefaultLanguage { get; set; }

    /// <summary>When false (default), comic reading is rejected in NSFW channels.</summary>
    public bool ComicNsfwAllowed { get; set; }

    /// <summary>
    /// Monotonic config-binding revision for the persisted value (newest snapshot). Set when loading from the config store API; cleared before writes so it is not stored inside
    /// binding JSON.
    /// </summary>
    public int? Revision { get; set; }

    /// <summary>Apply upgrades for stored JSON older than <see cref="CurrentSchemaVersion" /> (e.g. missing <see cref="Version" />).</summary>
    public void NormalizeForRead()
    {
        if (Version <= 0)
            Version = CurrentSchemaVersion;

        if (Version == 1)
            Version = 2;

        if (Version == 2)
            Version = 3;
    }

    /// <summary>Call before persisting so stored documents always carry the current schema version.</summary>
    public void NormalizeForPersistence()
    {
        NormalizeForRead();
        Version = CurrentSchemaVersion;
    }

    public override string ToString() => $"DiscordGuildSettings: v{Version}, commandChannel={CommandChannelId}, logChannel={LogChannelId}";
}