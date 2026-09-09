using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Drift.Web.Components;

/// <summary>Service registration for Drift UI palettes.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds Drift instance, snapshot-kind, diff-source, and file-change palettes so <see cref="LyoStatusChip" /> matches
        /// <see cref="DriftColorHelper" />. Skip this and a chip uses the shared status vocabulary instead.
        /// </summary>
        public IServiceCollection AddLyoDriftStatusPalettes()
        {
            ArgumentHelpers.ThrowIfNull(services);
            return services
                .AddLyoStatusPalette<DriftInstanceStatusPalette>()
                .AddLyoStatusPalette<DriftSnapshotKindPalette>()
                .AddLyoStatusPalette<DriftDiffSourcePalette>()
                .AddLyoStatusPalette<DriftChangeKindPalette>();
        }
    }
}
