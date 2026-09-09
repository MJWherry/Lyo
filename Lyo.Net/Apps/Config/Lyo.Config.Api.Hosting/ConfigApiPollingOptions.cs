using Lyo.Exceptions;

namespace Lyo.Config.Api.Hosting;

/// <summary>Binding and poll defaults for resolved config pulled from Config API.</summary>
public sealed class ConfigApiPollingOptions
{
    public const string SectionName = "ConfigApiPolling";

    public bool Enabled { get; set; } = true;

    /// <summary>Kind segment for Config API routes (<c>/api/config/{kind}/{id}</c>).</summary>
    public string AppKind { get; set; } = string.Empty;

    /// <summary>Id segment for Config API routes (<c>/api/config/{kind}/{id}</c>).</summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>Wait after HTTP 304 (<see cref="Lyo.Config.Api.Models.ConfigResolveOutcome.NotModified" />).</summary>
    public TimeSpan DelayWhenNotModified { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Wall-clock cap on the blocking first probe in <see cref="IHostedService.StartAsync(System.Threading.CancellationToken)" />. Omit or set non-positive to drop
    /// the deadline.
    /// </summary>
    public TimeSpan? StartupTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>When true, startup fails if <see cref="StartupTimeout" /> expires before a successful 200 snapshot.</summary>
    public bool RequireSuccessOnStartup { get; set; } = true;

    internal void ThrowIfMisconfiguredWhenEnabled()
    {
        if (!Enabled)
            return;

        OperationHelpers.ThrowIfNullOrWhiteSpace(AppKind);
        OperationHelpers.ThrowIfNullOrWhiteSpace(AppId);
    }
}