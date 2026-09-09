using Lyo.Testing.Containers;
using RabbitMQ.Client;

namespace Lyo.MessageQueue.RabbitMq.Tests;

/// <summary>
/// Shared RabbitMQ broker for this assembly's integration tests (management image so peek and queue stats work). Each test builds its own
/// <see cref="RabbitMqService" /> through <see cref="CreateService" /> so publisher confirms and per-queue limits can differ.
/// </summary>
public sealed class RabbitMqBrokerFixture : RabbitMqContainerFixtureBase
{
    /// <summary>Builds a service against the shared broker. Init-only endpoints come from the container; <paramref name="configure" /> adjusts the rest.</summary>
    public RabbitMqService CreateService(Action<RabbitMqOptions>? configure = null)
    {
        var options = new RabbitMqOptions {
            Host = Host,
            Port = Port,
            AdminUrl = AdminUrl,
            Username = Username,
            Password = Password
        };

        configure?.Invoke(options);
        var factory = new ConnectionFactory {
            HostName = options.Host,
            Port = options.Port,
            VirtualHost = options.VirtualHost,
            UserName = options.Username,
            Password = options.Password,
            AutomaticRecoveryEnabled = options.AutomaticRecovery,
            TopologyRecoveryEnabled = options.AutomaticRecovery,
            NetworkRecoveryInterval = options.NetworkRecoveryInterval
        };

        return new(options, factory);
    }
}