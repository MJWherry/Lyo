using Lyo.Testing.Containers;
using RabbitMQ.Client;

namespace Lyo.MessageQueue.RabbitMq.Tests;

/// <summary>
/// Shared RabbitMQ broker for all integration tests in this assembly (management-enabled image so peek and queue statistics work). Each test creates its own
/// <see cref="RabbitMqService" /> via <see cref="CreateService" /> so options such as publisher confirms and per-queue limits can vary per test.
/// </summary>
public sealed class RabbitMqBrokerFixture : RabbitMqContainerFixtureBase
{
    /// <summary>Creates a new service instance against the shared broker. Init-only endpoint options come from the container; <paramref name="configure" /> tweaks the rest.</summary>
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
