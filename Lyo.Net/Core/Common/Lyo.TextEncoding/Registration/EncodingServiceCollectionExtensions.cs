using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.TextEncoding.Registration;

/// <summary>Registers encoding services for dependency injection.</summary>
public static class EncodingServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds <see cref="IBinaryEncodingService" />: uses <see cref="BinaryEncodingService.Shared" /> when <paramref name="configure" /> is null;
        /// otherwise registers a configured singleton.
        /// </summary>
        public IServiceCollection AddLyoBinaryEncoding(Action<BinaryEncodingOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            if (configure is null) {
                services.AddSingleton<IBinaryEncodingService>(_ => BinaryEncodingService.Shared);
                return services;
            }

            services.AddSingleton(_ => {
                var o = new BinaryEncodingOptions();
                configure(o);
                return o;
            });
            services.AddSingleton<IBinaryEncodingService, BinaryEncodingService>();
            return services;
        }

        /// <summary>Adds <see cref="IBinaryEncodingService" /> with an explicit options instance.</summary>
        public IServiceCollection AddLyoBinaryEncoding(BinaryEncodingOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddSingleton(options);
            services.AddSingleton<IBinaryEncodingService, BinaryEncodingService>();
            return services;
        }

        /// <summary>
        /// Adds <see cref="ICharsetEncodingService" />: uses <see cref="CharsetEncodingService.Shared" /> when <paramref name="configure" /> is null;
        /// otherwise registers a configured singleton.
        /// </summary>
        public IServiceCollection AddLyoCharsetEncoding(Action<CharsetEncodingOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            if (configure is null) {
                services.AddSingleton<ICharsetEncodingService>(_ => CharsetEncodingService.Shared);
                return services;
            }

            services.AddSingleton(_ => {
                var o = new CharsetEncodingOptions();
                configure(o);
                return o;
            });
            services.AddSingleton<ICharsetEncodingService, CharsetEncodingService>();
            return services;
        }

        /// <summary>Adds <see cref="ICharsetEncodingService" /> with an explicit options instance.</summary>
        public IServiceCollection AddLyoCharsetEncoding(CharsetEncodingOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddSingleton(options);
            services.AddSingleton<ICharsetEncodingService, CharsetEncodingService>();
            return services;
        }

        /// <summary>Registers both binary and charset encoding services with defaults.</summary>
        public IServiceCollection AddLyoTextEncoding()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddLyoBinaryEncoding();
            services.AddLyoCharsetEncoding();
            return services;
        }

        /// <summary>Registers both services with optional configure callbacks.</summary>
        public IServiceCollection AddLyoTextEncoding(Action<BinaryEncodingOptions>? configureBinary, Action<CharsetEncodingOptions>? configureCharset)
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddLyoBinaryEncoding(configureBinary);
            services.AddLyoCharsetEncoding(configureCharset);
            return services;
        }
    }
}
