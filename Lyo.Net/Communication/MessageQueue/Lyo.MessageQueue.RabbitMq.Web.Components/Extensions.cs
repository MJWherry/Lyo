using Lyo.Exceptions;
using Lyo.Web.Primitives;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.MessageQueue.RabbitMq.Web.Components;

/// <summary>DI helpers for RabbitMQ UI components.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the broker queue-state palette so <see cref="LyoStatusChip" /> paints queue states with the same colours as
        /// <see cref="RabbitMqColorHelper.ForState" />. Optional: skip it and a chip uses the shared status vocabulary.
        /// </summary>
        public IServiceCollection AddLyoRabbitMqStatusPalette()
        {
            ArgumentHelpers.ThrowIfNull(services);
            return services.AddLyoStatusPalette<RabbitMqStatusPalette>();
        }
    }
}
