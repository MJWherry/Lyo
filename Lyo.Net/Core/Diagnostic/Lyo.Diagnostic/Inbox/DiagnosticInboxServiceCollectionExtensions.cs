using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Diagnostic.Inbox;

/// <summary>Registers the in-memory error inbox.</summary>
public static class DiagnosticInboxServiceCollectionExtensions
{
    /// <summary>Registers <see cref="InMemoryErrorInbox" /> as singleton for <see cref="IErrorOccurrenceSink" /> and <see cref="IErrorInboxReader" />.</summary>
    public static IServiceCollection AddInMemoryErrorInbox(this IServiceCollection services, Action<InMemoryErrorInboxOptions>? configure = null)
    {
        services.AddSingleton(_ => {
            var options = new InMemoryErrorInboxOptions();
            configure?.Invoke(options);
            return new InMemoryErrorInbox(options);
        });

        services.AddSingleton<IErrorOccurrenceSink>(sp => sp.GetRequiredService<InMemoryErrorInbox>());
        services.AddSingleton<IErrorInboxReader>(sp => sp.GetRequiredService<InMemoryErrorInbox>());
        return services;
    }
}