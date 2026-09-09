using Testcontainers.RabbitMq;

namespace Lyo.Testing.Containers;

/// <summary>Settings for <see cref="RabbitMqTestContainer" />.</summary>
public sealed class RabbitMqContainerOptions
{
    /// <summary>Docker image for RabbitMQ (default <c>rabbitmq:4-management-alpine</c>). Must be management-enabled for <see cref="RabbitMqTestContainer.AdminUrl" />.</summary>
    public string Image { get; set; } = "rabbitmq:4-management-alpine";

    /// <summary>Optional hook to tweak the Testcontainers builder before <see cref="RabbitMqBuilder.Build" />.</summary>
    public Action<RabbitMqBuilder>? ConfigureBuilder { get; set; }
}