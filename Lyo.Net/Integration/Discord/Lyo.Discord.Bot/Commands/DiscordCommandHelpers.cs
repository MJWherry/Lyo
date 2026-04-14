using DSharpPlus;
using DSharpPlus.SlashCommands;

namespace Lyo.Discord.Bot.Commands;

/// <summary>Guards and helpers for slash commands; throws <see cref="DiscordCommandException" /> for the global error handler.</summary>
public static class DiscordCommandHelpers
{
    /// <summary>Throws <see cref="DiscordCommandException" /> when <paramref name="value" /> is null.</summary>
    public static void ThrowIfNull<T>(T? value, string message)
        where T : class
    {
        if (value is null)
            throw new DiscordCommandException(message);
    }

    /// <summary>Requires a guild context (not DMs).</summary>
    public static void RequireGuild(InteractionContext ctx) => ThrowIfNull(ctx.Guild, "Use this command in a server.");

    /// <summary>Requires <see cref="Permissions.ManageGuild" /> on the invoking member.</summary>
    public static void RequireManageGuild(InteractionContext ctx)
    {
        RequireGuild(ctx);
        if (ctx.Member is null || !ctx.Member.Permissions.HasPermission(Permissions.ManageGuild))
            throw new DiscordCommandException("You need **Manage Server** permission to change settings.");
    }
}