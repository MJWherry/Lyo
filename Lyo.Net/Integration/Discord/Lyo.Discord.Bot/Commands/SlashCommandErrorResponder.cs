using System.Reflection;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using Lyo.Discord.Bot.Services;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;

namespace Lyo.Discord.Bot.Commands;

/// <summary>Handles <see cref="SlashCommandsExtension.SlashCommandErrored" />: logs, then replies ephemeral (or follow-up) with a safe message.</summary>
public static class SlashCommandErrorResponder
{
    private const string GenericUserMessage = "Something went wrong while running that command.";

    /// <summary>Subscribes <paramref name="slash" /> to log failures and respond with an ephemeral message when possible.</summary>
    public static void Subscribe(SlashCommandsExtension slash, ILogger logger, DiscordClient client, IGuildDiscordNotificationService? guildNotifications = null)
    {
        ArgumentHelpers.ThrowIfNull(slash, nameof(slash));
        ArgumentHelpers.ThrowIfNull(logger, nameof(logger));
        ArgumentHelpers.ThrowIfNull(client, nameof(client));
        slash.SlashCommandErrored += (_, e) => HandleAsync(e, logger, client, guildNotifications);
    }

    private static async Task HandleAsync(SlashCommandErrorEventArgs e, ILogger logger, DiscordClient client, IGuildDiscordNotificationService? guildNotifications)
    {
        var ex = Unwrap(e.Exception);
        var ctx = e.Context;
        using (logger.BeginScope("SlashCommand {QualifiedName}", ctx.QualifiedName)) {
            if (ex is DiscordCommandException dce) {
                logger.LogWarning(dce, "Slash command failed: {Message}", dce.Message);
                await TryRespondEphemeralAsync(ctx, dce.Message, logger).ConfigureAwait(false);
                await TryNotifyGuildAsync(guildNotifications, client, ctx, "Slash command (handled)", dce.Message, null).ConfigureAwait(false);
                return;
            }

            logger.LogError(ex, "Slash command failed with an unexpected error");
            await TryRespondEphemeralAsync(ctx, GenericUserMessage, logger).ConfigureAwait(false);
            await TryNotifyGuildAsync(guildNotifications, client, ctx, "Slash command (unexpected error)", null, ex).ConfigureAwait(false);
        }
    }

    private static async Task TryNotifyGuildAsync(
        IGuildDiscordNotificationService? notifications,
        DiscordClient client,
        InteractionContext ctx,
        string title,
        string? messageBody,
        Exception? exception)
    {
        if (notifications == null || ctx.Guild == null)
            return;

        try {
            if (exception != null)
                await notifications.NotifyGuildLogErrorAsync(client, ctx.Guild.Id, exception, $"Slash: {ctx.QualifiedName}").ConfigureAwait(false);
            else if (messageBody is { Length: > 0 })
                await notifications.NotifyGuildLogMessageAsync(client, ctx.Guild.Id, title, messageBody).ConfigureAwait(false);
        }
        catch {
            // notifier already logs; never fail the interaction path
        }
    }

    /// <summary>TargetInvocationException and AggregateException are flattened to the root failure.</summary>
    private static Exception Unwrap(Exception ex)
    {
        if (ex is TargetInvocationException { InnerException: { } inner })
            return Unwrap(inner);

        if (ex is AggregateException agg && agg.InnerExceptions.Count == 1)
            return Unwrap(agg.InnerExceptions[0]);

        return ex;
    }

    /// <summary>Replies with an ephemeral message, or a follow-up if the interaction was already acknowledged.</summary>
    private static async Task TryRespondEphemeralAsync(InteractionContext ctx, string message, ILogger logger)
    {
        try {
            await ctx.CreateResponseAsync(message, true).ConfigureAwait(false);
        }
        catch {
            try {
                var followUp = new DiscordFollowupMessageBuilder().WithContent(message).AsEphemeral();
                await ctx.FollowUpAsync(followUp).ConfigureAwait(false);
            }
            catch (Exception ex) {
                logger.LogWarning(ex, "Could not send slash command error response");
            }
        }
    }
}