using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.ApiEndpoint.Config;
using Lyo.Config;
using Lyo.Discord.Models;
using Lyo.Discord.Models.Request;
using Lyo.Discord.Models.Response;
using Lyo.Discord.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Postgres;
using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Discord.Postgres;

/// <summary>Extension methods for PostgreSQL Discord database context registration and API mapping.</summary>
public static class Extensions
{
    /// <summary>DB audit columns on <see cref="Database.DiscordUser" />; set via API CRUD hooks, not request DTOs.</summary>
    private static readonly CrudConfiguration<DiscordDbContext, DiscordUser, DiscordUserReq> DiscordUserCrud = new() {
        BeforeCreate = ctx => {
            var utc = DateTime.UtcNow;
            ctx.Entity.CreatedTimestamp = utc;
            ctx.Entity.UpdatedTimestamp = utc;
        },
        BeforeUpdate = ctx => ctx.Entity.UpdatedTimestamp = DateTime.UtcNow
    };

    /// <summary>DB audit columns on <see cref="Database.DiscordGuild" />; set via API CRUD hooks, not request DTOs.</summary>
    private static readonly CrudConfiguration<DiscordDbContext, DiscordGuild, DiscordGuildReq> DiscordGuildCrud = new() {
        BeforeCreate = ctx => {
            var utc = DateTime.UtcNow;
            ctx.Entity.CreatedTimestamp = utc;
            ctx.Entity.UpdatedTimestamp = utc;
        },
        BeforeUpdate = ctx => ctx.Entity.UpdatedTimestamp = DateTime.UtcNow,
        AfterUpsert = ctx => {
            var store = ctx.Services.GetService(typeof(IConfigStore)) as IConfigStore;
            if (store == null)
                return;

            DiscordGuildSettingsHelper.EnsureDefaultBindingAsync(store, ctx.Entity.Id).GetAwaiter().GetResult();
        }
    };

    private static readonly CrudConfiguration<DiscordDbContext, DiscordChannel, DiscordChannelReq> DiscordChannelCrud = new() {
        BeforeCreate = ctx => {
            var utc = DateTime.UtcNow;
            ctx.Entity.CreatedTimestamp = utc;
            ctx.Entity.UpdatedTimestamp = utc;
        },
        BeforeUpdate = ctx => ctx.Entity.UpdatedTimestamp = DateTime.UtcNow
    };

    private static readonly CrudConfiguration<DiscordDbContext, DiscordEmoji, DiscordEmojiReq> DiscordEmojiCrud = new() {
        BeforeCreate = ctx => {
            var utc = DateTime.UtcNow;
            ctx.Entity.CreatedTimestamp = utc;
            ctx.Entity.UpdatedTimestamp = utc;
        },
        BeforeUpdate = ctx => ctx.Entity.UpdatedTimestamp = DateTime.UtcNow
    };

    private static readonly CrudConfiguration<DiscordDbContext, DiscordRole, DiscordRoleReq> DiscordRoleCrud = new() {
        BeforeCreate = ctx => {
            var utc = DateTime.UtcNow;
            ctx.Entity.CreatedTimestamp = utc;
            ctx.Entity.UpdatedTimestamp = utc;
        },
        BeforeUpdate = ctx => ctx.Entity.UpdatedTimestamp = DateTime.UtcNow
    };

    private static readonly CrudConfiguration<DiscordDbContext, DiscordMessage, DiscordMessageReq> DiscordMessageCrud = new() {
        BeforeCreate = ctx => {
            var utc = DateTime.UtcNow;
            ctx.Entity.CreatedTimestamp = utc;
            ctx.Entity.UpdatedTimestamp = utc;
        },
        BeforeUpdate = ctx => ctx.Entity.UpdatedTimestamp = DateTime.UtcNow
    };

    /// <summary>Adds <see cref="DiscordDbContext" /> to the service collection.</summary>
    public static IServiceCollection AddDiscordDbContext(this IServiceCollection services, string connectionString)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));
        return services.AddDiscordDbContextFactory(new PostgresDiscordOptions { ConnectionString = connectionString })
            .AddScoped<DiscordDbContext>(sp => sp.GetRequiredService<IDbContextFactory<DiscordDbContext>>().CreateDbContext());
    }

    /// <summary>Adds <see cref="DiscordDbContext" /> to the service collection.</summary>
    public static IServiceCollection AddDiscordDbContext(this IServiceCollection services, Action<DbContextOptionsBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddDbContext<DiscordDbContext>(configure);
        return services;
    }

    /// <summary>Adds PostgreSQL Discord <see cref="IDbContextFactory{TContext}" /> to the service collection.</summary>
    public static IServiceCollection AddDiscordDbContextFactory(this IServiceCollection services, Action<PostgresDiscordOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresDiscordOptions();
        configure(options);
        return services.AddDiscordDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL Discord <see cref="IDbContextFactory{TContext}" /> using configuration binding.</summary>
    public static IServiceCollection AddDiscordDbContextFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresDiscordOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresDiscordOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddDiscordDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL Discord <see cref="IDbContextFactory{TContext}" /> with optional auto-migrations.</summary>
    public static IServiceCollection AddDiscordDbContextFactory(this IServiceCollection services, PostgresDiscordOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        services.AddSingleton<IOptions<PostgresDiscordOptions>>(Options.Create(options));
        services.AddPostgresMigrations<DiscordDbContext, PostgresDiscordOptions>();
        services.AddDbContextFactory<DiscordDbContext>(dbOptions => dbOptions.UseNpgsql(
            options.ConnectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgresDiscordOptions.Schema)));

        return services;
    }

    /// <summary>
    /// Registers Discord PostgreSQL persistence: <see cref="IDbContextFactory{TContext}" />, optional migrations, and CRUD/query services. Requires <c>AddLyoQueryServices</c>,
    /// cache, and <see cref="MapsterMapper.IMapper" /> (configure mappings via <see cref="ConfigureDiscordMappings" />).
    /// </summary>
    public static IServiceCollection AddPostgresDiscord(this IServiceCollection services, Action<PostgresDiscordOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresDiscordOptions();
        configure(options);
        return services.AddPostgresDiscord(options);
    }

    /// <summary>Registers Discord PostgreSQL persistence using configuration binding.</summary>
    public static IServiceCollection AddPostgresDiscordFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresDiscordOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        var options = new PostgresDiscordOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddPostgresDiscord(options);
    }

    /// <summary>Registers Discord PostgreSQL persistence: <see cref="IDbContextFactory{TContext}" />, optional migrations, and CRUD/query services.</summary>
    public static IServiceCollection AddPostgresDiscord(this IServiceCollection services, PostgresDiscordOptions options)
    {
        services.AddDiscordDbContextFactory(options);
        services.AddLyoCrudServices<DiscordDbContext>();
        return services;
    }

    /// <summary>Registers config definition seeding for <see cref="DiscordGuildSettings" /> (requires <c>AddPostgresConfigStore</c>).</summary>
    public static IServiceCollection AddDiscordGuildSettingsInfrastructure(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        services.AddHostedService<DiscordGuildSettingsDefinitionSeeder>();
        return services;
    }

    /// <summary>Maps Discord REST endpoints (query, CRUD, export). Call after <see cref="AddPostgresDiscord" />.</summary>
    public static WebApplication BuildDiscordGroup(this WebApplication app)
    {
        app.CreateBuilder<DiscordDbContext, DiscordUser, DiscordUserReq, DiscordUserRes, long>(Constants.Rest.Discord.Users, "Discord")
            .WithCrud(ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate, DiscordUserCrud)
            .Build();

        app.CreateBuilder<DiscordDbContext, DiscordGuild, DiscordGuildReq, DiscordGuildRes, long>(Constants.Rest.Discord.Guilds, "Discord")
            .WithCrud(ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate, DiscordGuildCrud)
            .Build();

        app.CreateBuilder<DiscordDbContext, DiscordChannel, DiscordChannelReq, DiscordChannelRes, long>(Constants.Rest.Discord.Channels, "Discord")
            .WithCrud(ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate, DiscordChannelCrud)
            .Build();

        app.CreateBuilder<DiscordDbContext, DiscordEmoji, DiscordEmojiReq, DiscordEmojiRes, long>(Constants.Rest.Discord.Emojis, "Discord")
            .WithCrud(ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate, DiscordEmojiCrud)
            .Build();

        app.CreateBuilder<DiscordDbContext, DiscordRole, DiscordRoleReq, DiscordRoleRes, long>(Constants.Rest.Discord.Roles, "Discord")
            .WithCrud(ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate, DiscordRoleCrud)
            .Build();

        app.CreateBuilder<DiscordDbContext, DiscordInteraction, DiscordInteractionReq, DiscordInteractionRes, long>(Constants.Rest.Discord.Interactions, "Discord")
            .WithCrud(ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate, new())
            .Build();

        app.CreateBuilder<DiscordDbContext, DiscordMessage, DiscordMessageReq, DiscordMessageRes, long>(Constants.Rest.Discord.Messages, "Discord")
            .WithCrud(ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate, DiscordMessageCrud)
            .Build();

        app.CreateBuilder<DiscordDbContext, DiscordAttachment, DiscordAttachmentReq, DiscordAttachmentRes, long>(Constants.Rest.Discord.Attachments, "Discord")
            .WithCrud(ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate, new())
            .Build();

        // Composite PK (UserId, GuildId): no GET/DELETE by single `{id}` route — use Query, Upsert, or PATCH with Keys in the body (two values in PK order).
        app.CreateBuilder<DiscordDbContext, DiscordMember, DiscordMemberReq, DiscordMemberRes, long>(Constants.Rest.Discord.Members, "Discord")
            .WithCrud(
                ApiFeatureFlag.Query | ApiFeatureFlag.Upsert | ApiFeatureFlag.UpsertBulk | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate |
                ApiFeatureFlag.Patch | ApiFeatureFlag.PatchBulk | ApiFeatureFlag.PatchInheritsUpdate, new())
            .Build();

        app.MapDiscordGuildSettingsEndpoints();
        return app;
    }

    /// <summary>Configures Mapster mappings for Discord entities and API DTOs.</summary>
    public static TypeAdapterConfig ConfigureDiscordMappings(this TypeAdapterConfig config)
    {
        config.NewConfig<DiscordUserReq, DiscordUser>()
            .Map(dest => dest.Username, src => DiscordUsernameOrPlaceholder(src.Username))
            .Ignore(dest => dest.CreatedTimestamp)
            .Ignore(dest => dest.UpdatedTimestamp);

        config.NewConfig<DiscordUser, DiscordUserRes>();
        config.NewConfig<DiscordGuildReq, DiscordGuild>()
            .Map(dest => dest.Name, src => DiscordGuildNameOrPlaceholder(src.Name))
            .Ignore(dest => dest.CreatedTimestamp)
            .Ignore(dest => dest.UpdatedTimestamp);

        config.NewConfig<DiscordGuild, DiscordGuildRes>();
        config.NewConfig<DiscordChannelReq, DiscordChannel>().Ignore(dest => dest.CreatedTimestamp).Ignore(dest => dest.UpdatedTimestamp);
        config.NewConfig<DiscordChannel, DiscordChannelRes>();
        config.NewConfig<DiscordEmojiReq, DiscordEmoji>().Ignore(dest => dest.CreatedTimestamp).Ignore(dest => dest.UpdatedTimestamp);
        config.NewConfig<DiscordEmoji, DiscordEmojiRes>();
        config.NewConfig<DiscordRoleReq, DiscordRole>().Ignore(dest => dest.CreatedTimestamp).Ignore(dest => dest.UpdatedTimestamp);
        config.NewConfig<DiscordRole, DiscordRoleRes>();
        config.NewConfig<DiscordInteractionReq, DiscordInteraction>();
        config.NewConfig<DiscordInteraction, DiscordInteractionRes>();
        config.NewConfig<DiscordMessageReq, DiscordMessage>().Ignore(dest => dest.CreatedTimestamp).Ignore(dest => dest.UpdatedTimestamp);
        config.NewConfig<DiscordMessage, DiscordMessageRes>();
        config.NewConfig<DiscordAttachmentReq, DiscordAttachment>();
        config.NewConfig<DiscordAttachment, DiscordAttachmentRes>();
        config.NewConfig<DiscordMemberReq, DiscordMember>();
        config.NewConfig<DiscordMember, DiscordMemberRes>();
        return config;
    }

    /// <summary>Discord payloads can omit or null names; DB columns are NOT NULL (varchar limits from migrations).</summary>
    private static string DiscordUsernameOrPlaceholder(string? username)
    {
        const int maxLen = 35;
        var n = username?.Trim();
        if (string.IsNullOrEmpty(n))
            return "(unknown)";

        return n.Length > maxLen ? n[..maxLen] : n;
    }

    private static string DiscordGuildNameOrPlaceholder(string? name)
    {
        const int maxLen = 50;
        var n = name?.Trim();
        if (string.IsNullOrEmpty(n))
            return "(unknown)";

        return n.Length > maxLen ? n[..maxLen] : n;
    }
}