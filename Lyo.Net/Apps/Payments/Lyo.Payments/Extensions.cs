using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Payments;

/// <summary>DI registration for the internal payment collector.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="InternalPaymentCollector" /> as <see cref="IPaymentCollector" />.</summary>
        public IServiceCollection AddInternalPaymentCollector()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddSingleton<IPaymentCollector, InternalPaymentCollector>();
            return services;
        }
    }
}
