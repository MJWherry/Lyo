using System.Text.Json.Serialization;
using Lyo.Api.Client;
using Lyo.Discord.Client.Managers;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Discord.Client;

/// <summary>HTTP client for Discord entities exposed by the Lyo API (PostgreSQL-backed DTOs).</summary>
public class LyoDiscordClient : ApiClient
{
    public readonly AttachmentManager Attachments;
    public readonly ChannelManager Channels;
    public readonly EmojiManager Emojis;
    public readonly GuildManager Guilds;
    public readonly InteractionManager Interactions;
    public readonly MemberManager Members;
    public readonly MessageManager Messages;
    public readonly RoleManager Roles;
    public readonly UserManager Users;

    public LyoDiscordClient(LyoDiscordClientOptions options, ILogger<LyoDiscordClient>? logger = null, HttpClient? httpClient = null)
        : base(
            logger ?? NullLogger<LyoDiscordClient>.Instance, httpClient ?? new HttpClient { BaseAddress = new($"{options.Url}") },
            new() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } }, options.ToApiClientOptions())
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        if (HttpClient.BaseAddress == null && !string.IsNullOrWhiteSpace(options.Url))
            HttpClient.BaseAddress = new(options.Url.TrimEnd('/') + "/");

        Guilds = new(this);
        Users = new(this);
        Channels = new(this);
        Roles = new(this);
        Emojis = new(this);
        Interactions = new(this);
        Messages = new(this);
        Attachments = new(this);
        Members = new(this);
    }
}