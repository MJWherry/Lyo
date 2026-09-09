using Lyo.Exceptions;
using Lyo.Web.Primitives;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Lyo.Privacy.Web.Components;

/// <summary>Registers the Privacy UI components with DI.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="PrivacyWorkbench" /> to the shared workbench registry under Infrastructure.</summary>
        public IServiceCollection AddLyoPrivacyWorkbench()
        {
            ArgumentHelpers.ThrowIfNull(services);
            return services.AddLyoWorkbench<PrivacyWorkbench>("Privacy redaction", Icons.Material.Filled.PrivacyTip, "Infrastructure", "privacy-redaction");
        }
    }
}
