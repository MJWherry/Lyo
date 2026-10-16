using Lyo.FeatureFlags;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.FeatureFlags.Web.Components;

/// <summary>
/// Lists every flag's name and mode. When <see cref="AllowEdit" /> is true, a switch sets that flag on or off through <see cref="IFeatureFlagUpdater" />.
/// A percentage or rules flag loses that targeting when the switch changes. Register <c>AddFeatureFlags</c> or <c>AddFeatureFlagsFromConfiguration</c> on the host.
/// </summary>
public partial class FeatureFlagPanel : IDisposable
{
    private IDisposable? _subscription;
    private IReadOnlyList<Row> _rows = [];

    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    /// <summary>Shows the on/off switch. Default is read-only.</summary>
    [Parameter]
    public bool AllowEdit { get; set; }

    private IFeatureFlagService? Service => Services.GetService<IFeatureFlagService>();

    private IOptionsMonitor<FeatureFlagOptions>? Monitor => Services.GetService<IOptionsMonitor<FeatureFlagOptions>>();

    /// <inheritdoc />
    public void Dispose() => _subscription?.Dispose();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        var monitor = Monitor;
        if (monitor is null)
            return;

        _subscription = monitor.OnChange((_, _) => InvokeAsync(() => {
            Load();
            StateHasChanged();
        }));
        Load();
    }

    private void Load()
    {
        var flags = Monitor?.CurrentValue.Flags;
        if (flags is null)
        {
            _rows = [];
            return;
        }

        var rows = new List<Row>(flags.Count);
        foreach (var pair in flags)
            rows.Add(new Row(pair.Key, pair.Value.Mode));

        _rows = rows;
    }

    private Task SetAsync(string name, bool enabled)
    {
        var monitor = Monitor;
        var updater = Services.GetService<IFeatureFlagUpdater>();
        if (monitor is null || updater is null)
            return Task.CompletedTask;

        var next = FeatureFlagUpdate.WithEnabled(monitor.CurrentValue, name, enabled);
        if (next is not null)
            updater.TryApply(next);

        Load();
        return Task.CompletedTask;
    }

    private readonly record struct Row(string Name, FeatureFlagMode Mode);
}
