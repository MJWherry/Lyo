using Lyo.Exceptions;
using Lyo.Hashing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.RequestSigning;

/// <summary>DI registration for <see cref="RequestSigner" />.</summary>
public static class Extensions
{
    /// <param name="services">Collection that receives the signer.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="RequestSigner" />. The HMAC key comes from <see cref="RequestSignatureOptions" />.</summary>
        public IServiceCollection AddRequestSigning(Action<RequestSignatureOptions> configure, Func<DateTime>? utcNow = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new RequestSignatureOptions();
            configure(options);
            return services.AddRequestSigning(options, utcNow);
        }

        /// <summary>Binds <see cref="RequestSignatureOptions" /> and registers the signer.</summary>
        public IServiceCollection AddRequestSigningFromConfiguration(IConfiguration configuration, string sectionName = RequestSignatureOptions.SectionName, Func<DateTime>? utcNow = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new RequestSignatureOptions();
            configuration.GetSection(sectionName).Bind(options);
            return services.AddRequestSigning(options, utcNow);
        }

        /// <summary>Registers <see cref="RequestSigner" />. <see cref="IHashingService" /> defaults to <see cref="HashingService.Shared" /> when the host has not registered one.</summary>
        public IServiceCollection AddRequestSigning(RequestSignatureOptions options, Func<DateTime>? utcNow = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(Options.Create(options));
            services.TryAddSingleton(options);
            services.TryAddSingleton<IHashingService>(HashingService.Shared);
            services.AddSingleton(sp => new RequestSigner(sp.GetRequiredService<IHashingService>(), options, utcNow));
            return services;
        }
    }
}
