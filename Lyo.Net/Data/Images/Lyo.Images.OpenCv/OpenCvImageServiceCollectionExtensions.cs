using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Images.OpenCv;

/// <summary>DI registration for OpenCV-backed image helpers.</summary>
public static class OpenCvImageServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="IOpenCvRoiInpaint" /> as a singleton <see cref="OpenCvRoiInpaintService" /> if not already registered.</summary>
        public IServiceCollection AddOpenCvRoiInpaint()
        {
            ArgumentHelpers.ThrowIfNull(services);
            if (!services.Any(static d => d.ServiceType == typeof(IOpenCvRoiInpaint)))
                services.AddSingleton<IOpenCvRoiInpaint, OpenCvRoiInpaintService>();

            return services;
        }
    }
}