using System.Diagnostics;
using Lyo.Api.Models.Common.Request;
using Lyo.Exceptions;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;

namespace Lyo.Api.Models.Builders;

[DebuggerDisplay("{ToString(),nq}")]
public class DeleteRequestBuilder
{
    private readonly List<ConditionClause> _identifiers = [];
    private readonly List<object[]> _keys = [];
    private bool _allowMultiple;

    public DeleteRequestBuilder WithKey(object key, params object[] extraKeys)
    {
        ArgumentHelpers.ThrowIfNull(key, nameof(key));
        _keys.Add(extraKeys.Length == 0 ? [key] : [key, ..extraKeys]);
        return this;
    }

    public DeleteRequestBuilder WithKeys(IEnumerable<object[]> keys)
    {
        ArgumentHelpers.ThrowIfNull(keys, nameof(keys));
        foreach (var k in keys) {
            ArgumentHelpers.ThrowIf(k == null || k.Length == 0, "Each key must contain at least one value", nameof(keys));
            _keys.Add(k!);
        }

        return this;
    }

    public DeleteRequestBuilder WithIdentifier(string propertyName, ComparisonOperatorEnum comparator, object? value, bool overwrite = false)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(propertyName, nameof(propertyName));
        if (overwrite)
            _identifiers.RemoveAll(f => string.Equals(f.Field, propertyName, StringComparison.OrdinalIgnoreCase) && f.Comparison == comparator);

        _identifiers.Add(new(propertyName, comparator, value));
        return this;
    }

    public DeleteRequestBuilder WithId(string propertyName, object value, bool overwrite = false) => WithIdentifier(propertyName, ComparisonOperatorEnum.Equals, value, overwrite);

    public DeleteRequestBuilder WithId(object value, bool overwrite = false) => WithId("Id", value, overwrite);

    public DeleteRequestBuilder WithIdentifiers(IEnumerable<ConditionClause> identifiers)
    {
        ArgumentHelpers.ThrowIfNull(identifiers, nameof(identifiers));
        foreach (var identifier in identifiers)
            _identifiers.Add(identifier);

        return this;
    }

    public DeleteRequestBuilder RemoveIdentifier(string propertyName)
    {
        _identifiers.RemoveAll(f => string.Equals(f.Field, propertyName, StringComparison.OrdinalIgnoreCase));
        return this;
    }

    public DeleteRequestBuilder ClearIdentifiers()
    {
        _identifiers.Clear();
        return this;
    }

    public DeleteRequestBuilder ClearKeys()
    {
        _keys.Clear();
        return this;
    }

    public DeleteRequestBuilder AllowMultiple(bool allow = true)
    {
        _allowMultiple = allow;
        return this;
    }

    public DeleteRequest Build()
    {
        // Keys and Identifiers are optional; at least one should be present unless caller explicitly wants an empty delete
        OperationHelpers.ThrowIf(_keys.Count == 0 && _identifiers.Count == 0, "At least one key or identifier must be specified");
        var identifiersNode = _identifiers.Count == 0 ? null : _identifiers.Count == 1 ? (WhereClause)_identifiers[0] : new GroupClause(GroupOperatorEnum.And, [.._identifiers]);
        return new() { Keys = _keys.Count > 0 ? _keys.ToList() : null, Query = identifiersNode, AllowMultiple = _allowMultiple };
    }

    public static DeleteRequestBuilder New() => new();

    public override string ToString() => $"Keys={_keys.Count} Identifiers={_identifiers.Count} AllowMultiple={_allowMultiple}";
}