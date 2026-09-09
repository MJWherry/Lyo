using Lyo.Exceptions;
using Lyo.Web.Primitives;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Lyo.Diagnostic.Web.Components;

/// <summary>DI registration for the Diagnostic UI components.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="DiagnosticWorkbench" /> with the shared workbench registry under Infrastructure.</summary>
        public IServiceCollection AddLyoDiagnosticWorkbench()
        {
            ArgumentHelpers.ThrowIfNull(services);
            return services.AddLyoWorkbench<DiagnosticWorkbench>("Diagnostics", Icons.Material.Filled.BugReport, "Infrastructure", "diagnostics");
        }
    }
}
