using System.IO.Compression;
using Lyo.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Api;

/// <summary>HTTP response compression and request-body decompression for Lyo API hosts.</summary>
public static class LyoApiCompressionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers Brotli and gzip response compression (HTTPS included) for almost all MIME types, plus request decompression for gzip/br/deflate bodies.
        /// Exclusions come from <see cref="LyoApiCompressionDefaults.ExcludedMimeTypes" />. Call <see cref="UseLyoApiCompression" /> in the pipeline.
        /// </summary>
        public IServiceCollection AddLyoApiCompression()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddResponseCompression(options => {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                options.MimeTypes = LyoApiCompressionDefaults.MimeTypes;
                options.ExcludedMimeTypes = LyoApiCompressionDefaults.ExcludedMimeTypes;
            });
            services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
            services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
            services.AddRequestDecompression();
            return services;
        }
    }

    extension(IApplicationBuilder app)
    {
        /// <summary>Compresses responses then decompresses request bodies. Place this before endpoints; after logging is fine.</summary>
        public IApplicationBuilder UseLyoApiCompression()
        {
            ArgumentHelpers.ThrowIfNull(app);
            app.UseResponseCompression();
            app.UseRequestDecompression();
            return app;
        }
    }
}
