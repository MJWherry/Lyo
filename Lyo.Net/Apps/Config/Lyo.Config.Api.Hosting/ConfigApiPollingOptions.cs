namespace Lyo.Config.Api.Hosting;

/// <summary>Binding and polling defaults for resolved config synced from Config API.</summary>
public sealed class ConfigApiPollingOptions
{
    public const string SectionName = "ConfigApiPolling";

    /// <summary>When false the hosted poll loop stays idle after <see cref="IHostedService.StartAsync" /> and the ledger is never populated by this component.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Passed to Config API routing (<c>/api/config/{kind}/{id}</c>).</summary>
    public string AppKind { get; set; } = string.Empty;

    /// <summary>Passed to Config API routing (<c>/api/config/{kind}/{id}</c>).</summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>Backoff after HTTP 304 (<see cref="Lyo.Config.Api.Models.ConfigResolveOutcome.NotModified"/>).</summary>
    public TimeSpan DelayWhenNotModified { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>Maximum wall-clock duration for the blocking first probe in <see cref="IHostedService.StartAsync(System.Threading.CancellationToken)" />;
    /// absent or non-positive disables the deadline.</summary>
    public TimeSpan? StartupTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>If true and <see cref="StartupTimeout"/> elapses without a successful 200 snapshot, startup fails.</summary>
    public bool RequireSuccessOnStartup { get; set; } = true;

    internal void ThrowIfMisconfiguredWhenEnabled()
    {
        if (!Enabled)
            return;

        if (string.IsNullOrWhiteSpace(AppKind))
            throw new InvalidOperationException($"Config API polling enabled but {nameof(AppKind)} is not set.");

        if (string.IsNullOrWhiteSpace(AppId))
            throw new InvalidOperationException($"Config API polling enabled but {nameof(AppId)} is not set.");
    }
}
