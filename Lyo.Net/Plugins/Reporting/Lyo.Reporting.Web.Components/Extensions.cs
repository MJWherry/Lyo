using Lyo.Exceptions;
using Lyo.Web.Primitives;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Reporting.Web.Components;

/// <summary>Service registration for Reporting UI components.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the report generation palette so <see cref="LyoStatusChip" /> paints generation statuses with the same colours as
        /// <see cref="ReportColorHelper" />. Skip this and a chip uses the shared status vocabulary instead.
        /// </summary>
        public IServiceCollection AddLyoReportStatusPalette()
        {
            ArgumentHelpers.ThrowIfNull(services);
            return services.AddLyoStatusPalette<ReportStatusPalette>();
        }
    }
}
