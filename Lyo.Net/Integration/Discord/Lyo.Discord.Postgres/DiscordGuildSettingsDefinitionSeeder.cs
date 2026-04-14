using Lyo.Config;
using Lyo.Discord.Models;
using Lyo.Discord.Postgres.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lyo.Discord.Postgres;

/// <summary>Registers the <see cref="DiscordGuildSettings" /> config definition for <see cref="DiscordGuild" /> (upsert idempotent).</summary>
public sealed class DiscordGuildSettingsDefinitionSeeder(IServiceProvider services) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var store = scope.ServiceProvider.GetService<IConfigStore>();
        if (store == null)
            return;

        await store.SaveDefinitionAsync(
                new() {
                    ForEntityType = typeof(DiscordGuild).FullName!,
                    Key = DiscordGuildSettingsHelper.Key,
                    ForValueType = ConfigValue.GetTypeName(typeof(DiscordGuildSettings)),
                    Description = "Discord guild bot settings (channels and roles).",
                    IsRequired = false
                }, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}