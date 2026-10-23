using Lyo.Exceptions;
using Lyo.FileStorage.Abstractions;
using Lyo.Web.Automation.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Web.Automation.FileStorage;

/// <summary>Registers <see cref="IAutomationPlanFileStorage" /> over the host's file storage service.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Replaces plan file storage with <paramref name="resolve" />. Call this after <c>AddWebAutomationPlanRunner</c>. The host chooses which <see cref="IFileStorageService" />
        /// to use, including a keyed service.
        /// </summary>
        public IServiceCollection AddAutomationPlanFileStorage(Func<IServiceProvider, IFileStorageService> resolve)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(resolve);
            services.Replace(
                ServiceDescriptor.Singleton<IAutomationPlanFileStorage>(sp => new FileStorageAutomationPlanFileStorage(sp.GetRequiredService<IServiceScopeFactory>(), resolve)));

            return services;
        }
    }
}