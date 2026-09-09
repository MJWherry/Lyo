using Lyo.Exceptions;
using Lyo.Ftp.Client;
using Lyo.IO.Temp.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.IO.Temp.Ftp;

/// <summary>DI helpers that plug FTP storage into IOTemp.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="FtpIOTempStorageProvider" /> as the singleton <see cref="IIOTempStorageProvider" />. Call this before <c>AddIOTempService</c>. Also registers
        /// <see cref="IFtpClient" /> when options are passed.
        /// </summary>
        public IServiceCollection AddIOTempFtpStorageProvider(Action<FtpClientOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddFtpClient(configure);
            return services.AddIOTempFtpStorageProvider();
        }

        /// <summary>Binds the FTP client from configuration, then registers the IOTemp storage provider.</summary>
        public IServiceCollection AddIOTempFtpStorageProviderFromConfiguration(IConfiguration configuration, string sectionName = FtpClientOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddFtpClientFromConfiguration(configuration, sectionName);
            return services.AddIOTempFtpStorageProvider();
        }

        /// <summary>Registers <see cref="FtpIOTempStorageProvider" /> against an <see cref="IFtpClient" /> already in DI.</summary>
        public IServiceCollection AddIOTempFtpStorageProvider()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<IIOTempStorageProvider>(sp => new FtpIOTempStorageProvider(sp.GetRequiredService<IFtpClient>()));
            return services;
        }
    }
}