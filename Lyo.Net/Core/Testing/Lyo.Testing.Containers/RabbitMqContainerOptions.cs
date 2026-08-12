using Testcontainers.RabbitMq;

namespace Lyo.Testing.Containers;

/// <summary>Configuration for <see cref="RabbitMqTestContainer" />.</summary>
public sealed class RabbitMqContainerOptions
{
    /// <summary>Docker image for RabbitMQ (default: rabbitmq:4-management-alpine). Must be a management-enabled image for <see cref="RabbitMqTestContainer.AdminUrl" /> to work.</summary>
    public string Image { get; set; } = "rabbitmq:4-management-alpine";

    /// <summary>Optional customization of the Testcontainers builder before <see cref="RabbitMqBuilder.Build" />.</summary>
    public Action<RabbitMqBuilder>? ConfigureBuilder { get; set; }
}