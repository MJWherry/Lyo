using Lyo.Exceptions;
using Lyo.IO.Temp.Storage;
using Lyo.Sftp.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.IO.Temp.Sftp;

/// <summary>DI helpers for SFTP-backed IOTemp storage.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="SftpIOTempStorageProvider" /> as the singleton <see cref="IIOTempStorageProvider" />. Call before <c>AddIOTempService</c>. Also registers
        /// <see cref="ISftpClient" /> when options are supplied.
        /// </summary>
        public IServiceCollection AddIOTempSftpStorageProvider(Action<SftpClientOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddSftpClient(configure);
            return services.AddIOTempSftpStorageProvider();
        }

        /// <summary>Registers SFTP client from configuration, then the IOTemp storage provider.</summary>
        public IServiceCollection AddIOTempSftpStorageProviderFromConfiguration(IConfiguration configuration, string sectionName = SftpClientOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddSftpClientFromConfiguration(configuration, sectionName);
            return services.AddIOTempSftpStorageProvider();
        }

        /// <summary>Registers <see cref="SftpIOTempStorageProvider" /> using an already-registered <see cref="ISftpClient" />.</summary>
        public IServiceCollection AddIOTempSftpStorageProvider()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<IIOTempStorageProvider>(sp => new SftpIOTempStorageProvider(sp.GetRequiredService<ISftpClient>()));
            return services;
        }
    }
}