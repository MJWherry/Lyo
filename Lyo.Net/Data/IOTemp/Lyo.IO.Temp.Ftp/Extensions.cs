using Lyo.Exceptions;
using Lyo.Ftp.Client;
using Lyo.IO.Temp.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.IO.Temp.Ftp;

/// <summary>DI helpers for FTP-backed IOTemp storage.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="FtpIOTempStorageProvider" /> as the singleton <see cref="IIOTempStorageProvider" />.
        /// Call before <c>AddIOTempService</c>. Also registers <see cref="IFtpClient" /> when options are supplied.
        /// </summary>
        public IServiceCollection AddIOTempFtpStorageProvider(Action<FtpClientOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddFtpClient(configure);
            return services.AddIOTempFtpStorageProvider();
        }

        /// <summary>Registers FTP client from configuration, then the IOTemp storage provider.</summary>
        public IServiceCollection AddIOTempFtpStorageProviderFromConfiguration(IConfiguration configuration, string sectionName = FtpClientOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddFtpClientFromConfiguration(configuration, sectionName);
            return services.AddIOTempFtpStorageProvider();
        }

        /// <summary>Registers <see cref="FtpIOTempStorageProvider" /> using an already-registered <see cref="IFtpClient" />.</summary>
        public IServiceCollection AddIOTempFtpStorageProvider()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<IIOTempStorageProvider>(sp => new FtpIOTempStorageProvider(sp.GetRequiredService<IFtpClient>()));
            return services;
        }
    }
}
