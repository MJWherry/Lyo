using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Job.Web.Components;

/// <summary>Service registration for Job UI components.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the job run and worker palettes so <see cref="LyoStatusChip" /> paints job statuses with the same colours and icons as
        /// <see cref="JobColorHelper" />. Skip this and a chip uses the shared status vocabulary instead.
        /// </summary>
        public IServiceCollection AddLyoJobStatusPalettes()
        {
            ArgumentHelpers.ThrowIfNull(services);
            return services.AddLyoStatusPalette<JobStatusPalette>().AddLyoStatusPalette<JobWorkerStatusPalette>();
        }
    }
}
