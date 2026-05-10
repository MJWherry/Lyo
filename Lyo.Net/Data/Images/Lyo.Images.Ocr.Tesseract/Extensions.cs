using Lyo.Exceptions;
using Lyo.Images.Ocr;
using Lyo.Images.Ocr.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Images.Ocr.Tesseract;

/// <summary>Registers <see cref="TesseractOcrEngine"/> as <see cref="IOcrEngine"/>.</summary>
/// <remarks>Not invoked by <c>Lyo.Images.Ocr.Tesseract.Tests</c>; tests use <c>TesseractOcrTestFixture</c> + manual engine constructors. Break here only when debugging host apps that call these extensions.</remarks>
public static class TesseractOcrServiceCollectionExtensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers Tesseract OCR using optional configuration callbacks.</summary>
        public IServiceCollection AddTesseractOcrEngine(Action<OcrEngineOptions>? configureShared = null, Action<TesseractOcrEngineOptions>? configureTesseract = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            if (services.Any(static d => d.ServiceType == typeof(IOcrEngine)))
                throw new InvalidOperationException($"{nameof(IOcrEngine)} is already registered.");

            if (!services.Any(static d => d.ServiceType == typeof(OcrEngineOptions))) {
                services.AddSingleton(_ => {
                    var o = new OcrEngineOptions();
                    configureShared?.Invoke(o);
                    return o;
                });
            }
            else if (configureShared != null) {
                throw new InvalidOperationException(
                    $"{nameof(OcrEngineOptions)} is already registered; remove the duplicate registration or configure it before calling {nameof(AddTesseractOcrEngine)}.");
            }

            services.AddSingleton(_ => {
                var t = new TesseractOcrEngineOptions();
                configureTesseract?.Invoke(t);
                return t;
            });

            services.AddSingleton<IOcrEngine, TesseractOcrEngine>();
            return services;
        }

        /// <summary>Binds <see cref="OcrEngineOptions"/> and <see cref="TesseractOcrEngineOptions"/> from configuration (nested under <c>Tesseract</c>).</summary>
        /// <remarks>
        /// Example:
        /// <code>
        /// "OcrEngine": {
        ///   "EnableMetrics": false,
        ///   "DefaultLanguages": "eng",
        ///   "Tesseract": { "TessdataDirectory": "/usr/share/tesseract-ocr/5/tessdata" }
        /// }
        /// </code>
        /// </remarks>
        public IServiceCollection AddTesseractOcrEngineFromConfiguration(IConfiguration configuration, string sectionName = OcrEngineOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(sectionName);
            if (services.Any(static d => d.ServiceType == typeof(IOcrEngine)))
                throw new InvalidOperationException($"{nameof(IOcrEngine)} is already registered.");

            if (!services.Any(static d => d.ServiceType == typeof(OcrEngineOptions))) {
                services.AddSingleton(_ => {
                    var o = new OcrEngineOptions();
                    configuration.GetSection(sectionName).Bind(o);
                    return o;
                });
            }

            services.AddSingleton(_ => {
                var t = new TesseractOcrEngineOptions();
                configuration.GetSection(sectionName).GetSection(TesseractOcrEngineOptions.ConfigurationKey).Bind(t);
                return t;
            });

            services.AddSingleton<IOcrEngine, TesseractOcrEngine>();
            return services;
        }
    }
}
