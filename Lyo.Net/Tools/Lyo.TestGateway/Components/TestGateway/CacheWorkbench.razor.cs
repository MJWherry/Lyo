using System.Net.Http.Json;
using Lyo.Api.Client;
using Lyo.Cache;
using Lyo.Common.Metadata.Records;
using Lyo.Health;
using Lyo.Images;
using Lyo.Images.Models;
using Lyo.Metrics;
using Lyo.TestGateway;
using Lyo.TestGateway.Components;
using Lyo.TestGateway.Models;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Lyo.Web.Components.DataGrid;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using MudBlazor;
using static Microsoft.AspNetCore.Components.Web.RenderMode;
using CacheConstants = Lyo.Cache.Constants;

namespace Lyo.TestGateway.Components.TestGateway;

public partial class CacheWorkbench
{
    private bool _busy;
    private string _cacheKey = "workbench:cache";
    private double? _cacheSizeGauge;
    private string _cacheTag = "workbench";
    private int _durationMilliseconds = 2500;
    private readonly List<DemoEvent> _events = [];
    private int _factoryCallCount;
    private int _factoryDelayMilliseconds = 150;
    private long _hitSuccess;
    private int _hitDurationCount;
    private double _hitDurationAverageMs;
    private string _inputValue = "hello from cache workbench";
    private List<CacheItem> _items = [];
    private HealthResult? _lastHealth;
    private CacheDemoValue? _lastValue;
    private DateTime _lastMetricsRefresh;
    private long _missSuccess;
    private int _missDurationCount;
    private double _missDurationAverageMs;
    private long _removeByTagItemsRemoved;
    private long _removeByTagSuccess;
    private long _removeSuccess;
    private int _removeDurationCount;
    private double _removeDurationAverageMs;
    private long _setSuccess;
    private int _setDurationCount;
    private double _setDurationAverageMs;

    private MetricsService? MetricsStore => Metrics as MetricsService;

    protected override Task OnInitializedAsync()
    {
        RefreshSnapshot();
        RefreshMetrics();
        return Task.CompletedTask;
    }

    private async Task SetValueAsync()
    {
        _busy = true;
        try {
            var payload = CreatePayload("set");
            CacheService.Set(_cacheKey, payload, [_cacheTag]);
            _lastValue = payload;
            AppendEvent("Set", $"Stored '{_cacheKey}' with tag '{_cacheTag}'.");
            RefreshSnapshot();
            RefreshMetrics();
            SetStatus("Value written to cache.", Severity.Success);
        }
        catch (Exception ex) {
            AppendEvent("Set Error", ex.Message);
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task ReadWithGetOrSetAsync()
    {
        _busy = true;
        try {
            var beforeFactoryCallCount = _factoryCallCount;
            var result = await CacheService.GetOrSetAsync(
                _cacheKey, async ct => {
                    var callNumber = Interlocked.Increment(ref _factoryCallCount);
                    if (_factoryDelayMilliseconds > 0)
                        await Task.Delay(_factoryDelayMilliseconds, ct);

                    return CreatePayload("factory", callNumber);
                }, TimeSpan.FromMilliseconds(_durationMilliseconds), [_cacheTag]);

            _lastValue = result;
            var hit = beforeFactoryCallCount == _factoryCallCount;
            AppendEvent(hit ? "Hit" : "Miss", $"{(hit ? "Returned cached value" : "Factory populated cache")} for '{_cacheKey}'.");
            RefreshSnapshot();
            RefreshMetrics();
            SetStatus(hit ? "Cache hit." : "Cache miss; factory executed.", hit ? Severity.Info : Severity.Success);
        }
        catch (Exception ex) {
            AppendEvent("GetOrSet Error", ex.Message);
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task RunExpirationDemoAsync()
    {
        _busy = true;
        try {
            await CacheService.InvalidateCacheItem(_cacheKey);
            AppendEvent("Expire Demo", $"Cleared '{_cacheKey}' to start from a miss.");
            var first = await CacheService.GetOrSetAsync(
                _cacheKey, async ct => {
                    var callNumber = Interlocked.Increment(ref _factoryCallCount);
                    if (_factoryDelayMilliseconds > 0)
                        await Task.Delay(_factoryDelayMilliseconds, ct);

                    return CreatePayload("expiration-demo", callNumber);
                }, TimeSpan.FromMilliseconds(_durationMilliseconds), [_cacheTag]);

            await Task.Delay(_durationMilliseconds + 150);
            var second = await CacheService.GetOrSetAsync(
                _cacheKey, async ct => {
                    var callNumber = Interlocked.Increment(ref _factoryCallCount);
                    if (_factoryDelayMilliseconds > 0)
                        await Task.Delay(_factoryDelayMilliseconds, ct);

                    return CreatePayload("expiration-demo", callNumber);
                }, TimeSpan.FromMilliseconds(_durationMilliseconds), [_cacheTag]);

            _lastValue = second;
            var expired = first?.FactoryCallNumber != second?.FactoryCallNumber;
            AppendEvent("Expire Demo", expired ? $"Entry expired after {_durationMilliseconds} ms and the second read rebuilt it." : "Entry did not appear to expire before the second read.");
            RefreshSnapshot();
            RefreshMetrics();
            SetStatus(expired ? "Expiration demo rebuilt the value after TTL." : "Expiration demo did not rebuild as expected.", expired ? Severity.Success : Severity.Warning);
        }
        catch (Exception ex) {
            AppendEvent("Expire Demo Error", ex.Message);
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task InvalidateKeyAsync()
    {
        _busy = true;
        try {
            await CacheService.InvalidateCacheItem(_cacheKey);
            AppendEvent("Invalidate Key", $"Removed '{_cacheKey}'.");
            RefreshSnapshot();
            RefreshMetrics();
            SetStatus("Cache key invalidated.", Severity.Success);
        }
        catch (Exception ex) {
            AppendEvent("Invalidate Key Error", ex.Message);
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task InvalidateTagAsync()
    {
        _busy = true;
        try {
            await CacheService.InvalidateCacheItemByTag(_cacheTag);
            AppendEvent("Invalidate Tag", $"Removed items tagged '{_cacheTag}'.");
            RefreshSnapshot();
            RefreshMetrics();
            SetStatus("Cache tag invalidated.", Severity.Success);
        }
        catch (Exception ex) {
            AppendEvent("Invalidate Tag Error", ex.Message);
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task CheckHealthAsync()
    {
        _busy = true;
        try {
            _lastHealth = await CacheService.CheckHealthAsync();
            AppendEvent("Health", _lastHealth.IsHealthy ? "Cache health check passed." : $"Cache health check failed: {_lastHealth.Message}");
            RefreshSnapshot();
            RefreshMetrics();
            SetStatus(_lastHealth.IsHealthy ? "Cache is healthy." : _lastHealth.Message ?? "Cache is unhealthy.", _lastHealth.IsHealthy ? Severity.Success : Severity.Warning);
        }
        catch (Exception ex) {
            AppendEvent("Health Error", ex.Message);
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private void RefreshSnapshot() => _items = CacheService.Items.OrderBy(i => i.Type).ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ToList();

    private void RefreshMetrics()
    {
        if (MetricsStore == null)
            return;

        var keyTags = new[] { (CacheConstants.Metrics.Tags.Key, _cacheKey) };
        var tagTags = new[] { (CacheConstants.Metrics.Tags.Tag, _cacheTag) };
        _hitSuccess = MetricsStore.GetCounterValue(CacheConstants.Metrics.HitSuccess, keyTags);
        _missSuccess = MetricsStore.GetCounterValue(CacheConstants.Metrics.MissSuccess, keyTags);
        _setSuccess = MetricsStore.GetCounterValue(CacheConstants.Metrics.SetSuccess, keyTags);
        _removeSuccess = MetricsStore.GetCounterValue(CacheConstants.Metrics.RemoveSuccess, keyTags);
        _removeByTagSuccess = MetricsStore.GetCounterValue(CacheConstants.Metrics.RemoveByTagSuccess, tagTags);
        _hitDurationCount = MetricsStore.GetHistogram(CacheConstants.Metrics.HitDuration, keyTags)?.Count ?? 0;
        _hitDurationAverageMs = MetricsStore.GetHistogram(CacheConstants.Metrics.HitDuration, keyTags)?.Average ?? 0;
        _missDurationCount = MetricsStore.GetHistogram(CacheConstants.Metrics.MissDuration, keyTags)?.Count ?? 0;
        _missDurationAverageMs = MetricsStore.GetHistogram(CacheConstants.Metrics.MissDuration, keyTags)?.Average ?? 0;
        _setDurationCount = MetricsStore.GetHistogram(CacheConstants.Metrics.SetDuration, keyTags)?.Count ?? 0;
        _setDurationAverageMs = MetricsStore.GetHistogram(CacheConstants.Metrics.SetDuration, keyTags)?.Average ?? 0;
        _removeDurationCount = MetricsStore.GetHistogram(CacheConstants.Metrics.RemoveDuration, keyTags)?.Count ?? 0;
        _removeDurationAverageMs = MetricsStore.GetHistogram(CacheConstants.Metrics.RemoveDuration, keyTags)?.Average ?? 0;
        _removeByTagItemsRemoved = Convert.ToInt64(MetricsStore.GetGaugeValue(CacheConstants.Metrics.RemoveByTagItemsRemoved, tagTags) ?? 0);
        _cacheSizeGauge = MetricsStore.GetGaugeValue(CacheConstants.Metrics.CacheSize);
        _lastMetricsRefresh = DateTime.UtcNow;
    }

    private void ClearEvents()
    {
        _events.Clear();
        SetStatus("Event log cleared.", Severity.Info);
    }

    private CacheDemoValue CreatePayload(string source, int? factoryCallNumber = null)
        => new() {
            CreatedAt = DateTime.UtcNow,
            FactoryCallNumber = factoryCallNumber ?? _factoryCallCount,
            Source = source,
            Text = _inputValue
        };

    private void AppendEvent(string stage, string message)
    {
        _events.Insert(0, new(DateTime.UtcNow, stage, message));
        if (_events.Count > 40)
            _events.RemoveRange(40, _events.Count - 40);
    }

    private sealed record DemoEvent(DateTime Timestamp, string Stage, string Message);

    private sealed class CacheDemoValue
    {
        public DateTime CreatedAt { get; init; }

        public int FactoryCallNumber { get; init; }

        public required string Source { get; init; }

        public required string Text { get; init; }
    }
}
