using Lyo.FileStorage.Web.Components.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.TestGateway.Services;

public static class FileStorageWorkbenchExtensions
{
    public static IServiceCollection AddFileStorageWorkbenchSupport(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileStorageWebOptions>(configuration.GetSection("FileStorageWorkbench"));
        return services;
    }
}
