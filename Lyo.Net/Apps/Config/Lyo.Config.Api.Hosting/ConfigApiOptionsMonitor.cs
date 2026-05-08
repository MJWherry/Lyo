using Lyo.Exceptions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Lyo.Config.Api.Hosting;

/// <summary><see cref="IOptionsMonitor{TOptions}" /> backed by a single definition key in the shared <see cref="ResolvedConfigRecord" /> ledger.</summary>
public sealed class ConfigApiOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
    where TOptions : class, new()
{
    private readonly string _definitionKey;

    private readonly object _gate = new();
    private readonly ConfigApiResolvedLedger _ledger;
    private readonly List<Action<TOptions, string?>> _listeners = [];
    private readonly ConfigApiMissingDefinitionKeyBehavior _missing;
    private readonly IDisposable _reloadSubscription;
    private TOptions _current = null!;

    private bool _initialized;

    public ConfigApiOptionsMonitor(ConfigApiResolvedLedger ledger, string definitionKey, ConfigApiMissingDefinitionKeyBehavior missingDefinitionKeyBehavior)
    {
        _ledger = ledger;
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(definitionKey);
        _missing = missingDefinitionKeyBehavior;
        _reloadSubscription = ChangeToken.OnChange(_ledger.GetReloadToken, Reload);
    }

    public TOptions CurrentValue {
        get {
            lock (_gate) {
                if (!_initialized)
                    Prime();

                return _current;
            }
        }
    }

    public TOptions Get(string? name)
    {
        if (string.IsNullOrEmpty(name) || string.Equals(Options.DefaultName, name, StringComparison.Ordinal))
            return CurrentValue;

        throw new InvalidOperationException($"Named options are not supported for Config API sources (requested name '{name}').");
    }

    /// <inheritdoc />
    /// <remarks>Immediately invokes <paramref name="listener" /> with the current cached value (<see cref="Options.DefaultName" />).</remarks>
    public IDisposable OnChange(Action<TOptions, string?> listener)
    {
        ArgumentHelpers.ThrowIfNull(listener);
        lock (_gate) {
            if (!_initialized)
                Prime();

            _listeners.Add(listener);
        }

        listener(CurrentValue, Options.DefaultName);
        return new Registration(this, listener);
    }

    private void Reload()
    {
        TOptions next;
        List<Action<TOptions, string?>> snapshot;
        lock (_gate) {
            next = Materialize(_ledger.Current);
            _current = next;
            _initialized = true;
            snapshot = _listeners.ToList();
        }

        foreach (var l in snapshot)
            l(next, Options.DefaultName);
    }

    private void Prime()
    {
        _current = Materialize(_ledger.Current);
        _initialized = true;
    }

    /// <remarks>Prefer <see cref="ResolvedConfigRecord.TryGetValue" /> so absent keys surface via <paramref name="missingDefinitionKeyBehavior" />.</remarks>
    private TOptions Materialize(ResolvedConfigRecord? record)
    {
        if (record == null) {
            return _missing == ConfigApiMissingDefinitionKeyBehavior.Throw
                ? throw new InvalidOperationException("No Config API snapshot is available yet (ledger empty). UseDefaultInstance disables this fault.")
                : new TOptions();
        }

        if (!record.TryGetValue(_definitionKey, out var configValue) || configValue == null) {
            return _missing == ConfigApiMissingDefinitionKeyBehavior.Throw
                ? throw new InvalidOperationException(
                    $"Definition key '{_definitionKey}' is missing from resolved Config API payload for '{record.ForEntityType}:{record.ForEntityId}'.")
                : new TOptions();
        }

        var deserialized = configValue.GetValue<TOptions>(ConfigJsonSerializerOptions.Default);
        return deserialized ?? _missing switch {
            ConfigApiMissingDefinitionKeyBehavior.Throw => throw new InvalidOperationException(
                $"JSON for definition key '{_definitionKey}' did not deserialize to {typeof(TOptions).Name}."),
            var _ => new()
        };
    }

    private sealed class Registration : IDisposable
    {
        private readonly Action<TOptions, string?> _listener;
        private ConfigApiOptionsMonitor<TOptions>? _owner;

        public Registration(ConfigApiOptionsMonitor<TOptions> owner, Action<TOptions, string?> listener)
        {
            _owner = owner;
            _listener = listener;
        }

        public void Dispose()
        {
            var owner = Interlocked.Exchange(ref _owner, null);
            if (owner == null)
                return;

            lock (owner._gate)
                owner._listeners.Remove(_listener);
        }
    }
}