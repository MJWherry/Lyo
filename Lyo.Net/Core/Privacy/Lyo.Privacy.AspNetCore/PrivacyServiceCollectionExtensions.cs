using Lyo.Metrics;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Configuration;
using Lyo.Privacy.Json;
using Lyo.Privacy.Policy;
using Lyo.Privacy.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Privacy.AspNetCore;

/// <summary>Registers <see cref="ITextRedactor" /> and <see cref="IStructuredRedactor" /> with the DI container.</summary>
public static class PrivacyServiceCollectionExtensions
{
    /// <summary>Binds <see cref="PrivacyRedactorOptions.SectionName" /> from configuration when <paramref name="configuration" /> is not null.</summary>
    public static IServiceCollection AddLyoPrivacy(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<PrivacyRedactorOptions>? configureOptions = null,
        Action<RedactionPolicyBuilder>? configureDefaultPolicy = null)
    {
        services.AddOptions<PrivacyRedactorOptions>();
        if (configuration is not null)
            services.Configure<PrivacyRedactorOptions>(configuration.GetSection(PrivacyRedactorOptions.SectionName));

        if (configureOptions is not null)
            services.Configure(configureOptions);

        services.AddSingleton<ITextRedactor>(sp => {
            var o = sp.GetRequiredService<IOptions<PrivacyRedactorOptions>>().Value;
            var metrics = sp.GetService<IMetrics>() ?? NullMetrics.Instance;
            return new TextRedactor(o.BuildTextPolicy(configureDefaultPolicy), metrics);
        });

        services.AddSingleton<IStructuredRedactor>(sp => {
            var o = sp.GetRequiredService<IOptions<PrivacyRedactorOptions>>().Value;
            var text = sp.GetRequiredService<ITextRedactor>();
            var metrics = sp.GetService<IMetrics>() ?? NullMetrics.Instance;
            return new JsonRedactor(o.BuildJsonOptions(), o.JsonApplyTextRulesToStrings ? text : null, metrics);
        });

        return services;
    }

    /// <summary>Registers a keyed <see cref="ITextRedactor" /> (for example <c>"Support"</c>).</summary>
    public static IServiceCollection AddLyoPrivacyPolicy(this IServiceCollection services, object serviceKey, Action<RedactionPolicyBuilder> configure)
    {
        if (configure is null)
            throw new ArgumentNullException(nameof(configure));

        services.AddKeyedSingleton<ITextRedactor>(
            serviceKey, (sp, _) => {
                var metrics = sp.GetService<IMetrics>() ?? NullMetrics.Instance;
                var b = new RedactionPolicyBuilder();
                configure(b);
                var policy = b.Build();
                if (policy.Name is null && serviceKey is not null)
                    policy = policy with { Name = serviceKey.ToString() };

                return new TextRedactor(policy, metrics);
            });

        return services;
    }
}