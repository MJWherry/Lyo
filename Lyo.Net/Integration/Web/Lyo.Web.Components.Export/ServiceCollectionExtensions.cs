using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Enums;
using Lyo.Web.Components.DataGrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Web.Components.Export;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLyoDataGridExport(this IServiceCollection services)
    {
        services.TryAddScoped<DataGridExportService>();
        return services;
    }
}
