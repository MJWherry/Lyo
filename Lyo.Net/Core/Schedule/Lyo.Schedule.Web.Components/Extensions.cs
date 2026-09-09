using Lyo.Exceptions;
using Lyo.Web.Primitives;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Lyo.Schedule.Web.Components;

/// <summary>Helpers that register the Schedule UI components.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="ScheduleWorkbench" /> on the shared workbench registry under Infrastructure.</summary>
        public IServiceCollection AddLyoScheduleWorkbench()
        {
            ArgumentHelpers.ThrowIfNull(services);
            return services.AddLyoWorkbench<ScheduleWorkbench>("Schedule", Icons.Material.Filled.Schedule, "Infrastructure", "schedule");
        }
    }
}
