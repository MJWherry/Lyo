using Lyo.Common;
using Lyo.Config;
using Lyo.Discord.Models;
using Lyo.Discord.Postgres.Database;
using Lyo.Exceptions;

namespace Lyo.Discord.Postgres;

/// <summary>Guild-scoped config key and helpers using <see cref="EntityRef.For{T}(object[])" /> for <see cref="DiscordGuild" />.</summary>
public static class DiscordGuildSettingsHelper
{
    /// <summary>Config definition / binding key for <see cref="DiscordGuildSettings" />.</summary>
    public const string Key = "GuildSettings";

    /// <summary>Entity type string used by <see cref="EntityRef.For{T}(object[])" /> for <see cref="DiscordGuild" /> (full CLR name).</summary>
    public static string DiscordGuildEntityType => typeof(DiscordGuild).FullName!;

    /// <summary>Resolves the <see cref="EntityRef" /> for a guild snowflake id.</summary>
    public static EntityRef GuildRef(long guildId) => EntityRef.For<DiscordGuild>(guildId);

    /// <summary>Ensures a config binding exists with all-null settings (idempotent).</summary>
    public static async Task EnsureDefaultBindingAsync(IConfigStore store, long guildId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(store, nameof(store));
        var r = GuildRef(guildId);
        if (await store.GetBindingAsync(r, Key, ct).ConfigureAwait(false) != null)
            return;

        var defaults = new DiscordGuildSettings();
        defaults.NormalizeForPersistence();
        await store.SaveBindingAsync(
                new() {
                    Key = Key,
                    ForEntityType = r.EntityType,
                    ForEntityId = r.EntityId,
                    Value = ConfigValue.From(defaults)
                }, ct)
            .ConfigureAwait(false);
    }

    /// <summary>Loads resolved guild settings, or default empty object when no binding.</summary>
    public static async Task<DiscordGuildSettings> GetSettingsAsync(IConfigStore store, long guildId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(store, nameof(store));
        await EnsureDefaultBindingAsync(store, guildId, ct).ConfigureAwait(false);
        var resolved = await store.LoadConfigAsync(GuildRef(guildId), ct).ConfigureAwait(false);
        var item = resolved.Items.FirstOrDefault(i => string.Equals(i.Definition.Key, Key, StringComparison.Ordinal));
        var settings = item?.Value?.GetValue<DiscordGuildSettings>() ?? new DiscordGuildSettings();
        settings.NormalizeForRead();
        var binding = await store.GetBindingAsync(GuildRef(guildId), Key, ct).ConfigureAwait(false);
        if (binding != null) {
            var revisions = await store.GetBindingRevisionsAsync(binding.Id, ct).ConfigureAwait(false);
            if (revisions.Count > 0)
                settings.Revision = revisions[0].Revision;
        }

        return settings;
    }
}