using Lyo.FileStorage.Web.Components.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.TestGateway.Services;

public static class FileStorageWorkbenchExtensions
{
    public static IServiceCollection AddFileStorageWorkbenchSupport(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileStorageWebOptions>(configuration.GetSection("FileStorageWorkbench"));
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<FileStorageWebOptions>>().Value);
        return services;
    }
}
