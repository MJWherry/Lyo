using Lyo.Configuration;
using Lyo.Exceptions;
using Lyo.Images.Ocr.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Images.Ocr;

/// <summary>DI helpers that register shared OCR configuration types (provider implementations register <see cref="IOcrEngine" />).</summary>
public static class OcrServiceCollectionExtensions
{
    /// <param name="services">Service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Binds <see cref="OcrEngineOptions" /> from configuration.</summary>
        public IServiceCollection AddOcrEngineOptionsFromConfiguration(IConfiguration configuration, string sectionName = OcrEngineOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(sectionName);
            services.AddSingleton(_ => {
                var options = LyoOptions.Bind<OcrEngineOptions>(configuration, sectionName);

                return options;
            });

            return services;
        }

        /// <summary>Registers <see cref="OcrEngineOptions" /> with optional setup.</summary>
        public IServiceCollection AddOcrEngineOptions(Action<OcrEngineOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            var options = new OcrEngineOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);
            return services;
        }
    }
}