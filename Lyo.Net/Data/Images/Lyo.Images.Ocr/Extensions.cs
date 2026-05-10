using Lyo.Images.Ocr.Models;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Images.Ocr;

/// <summary>Registers shared OCR configuration types (provider implementations register <see cref="IOcrEngine"/>).</summary>
public static class OcrServiceCollectionExtensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Binds <see cref="OcrEngineOptions"/> from configuration.</summary>
        public IServiceCollection AddOcrEngineOptionsFromConfiguration(IConfiguration configuration, string sectionName = OcrEngineOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(sectionName);
            services.AddSingleton(_ => {
                var options = new OcrEngineOptions();
                var section = configuration.GetSection(sectionName);
                if (section.Exists())
                    section.Bind(options);

                return options;
            });

            return services;
        }

        /// <summary>Registers <see cref="OcrEngineOptions"/> with optional setup.</summary>
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
