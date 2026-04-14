using System.Diagnostics;
using Lyo.Api.Models.Common.Request;
using Lyo.Exceptions;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;

namespace Lyo.Api.Models.Builders;

[DebuggerDisplay("{ToString(),nq}")]
public class PatchRequestBuilder
{
    private readonly List<ConditionClause> _identifiers = [];
    private readonly List<object[]> _keys = [];
    private readonly Dictionary<string, object?> _properties = new();
    private bool _allowMultiple;

    public PatchRequestBuilder WithIdentifier(string propertyName, ComparisonOperatorEnum comparator, object? value)
    {
        _identifiers.Add(new(propertyName, comparator, value));
        return this;
    }

    public PatchRequestBuilder WithOneOfIdentifier<T>(string propertyName, IEnumerable<T>? values)
    {
        _identifiers.Add(new(propertyName, ComparisonOperatorEnum.In, values));
        return this;
    }

    public PatchRequestBuilder WithKey(object key, params object[] extraKeys)
    {
        ArgumentHelpers.ThrowIfNull(key, nameof(key));
        _keys.Add(extraKeys.Length == 0 ? [key] : [key, ..extraKeys]);
        return this;
    }

    public PatchRequestBuilder WithKey(object[] key)
    {
        ArgumentHelpers.ThrowIfNull(key, nameof(key));
        if (key.Length == 0)
            throw new ArgumentException("Key must contain at least one value", nameof(key));

        _keys.Add(key);
        return this;
    }

    public PatchRequestBuilder WithId(object value) => WithKey(value);

    public PatchRequestBuilder WithIds<T>(IEnumerable<T> ids)
    {
        foreach (var id in ids)
            WithId(id!);

        return this;
    }

    public PatchRequestBuilder SetProperty(string propertyName, object? value)
    {
        _properties[propertyName] = value;
        return this;
    }

    public PatchRequestBuilder SetProperties(Dictionary<string, object?> properties)
    {
        foreach (var kvp in properties)
            _properties[kvp.Key] = kvp.Value;

        return this;
    }

    public PatchRequestBuilder SetProperties(object propertyObject)
    {
        var properties = propertyObject.GetType().GetProperties();
        foreach (var prop in properties)
            _properties[prop.Name] = prop.GetValue(propertyObject);

        return this;
    }

    public PatchRequestBuilder AllowMultiple(bool allow = true)
    {
        _allowMultiple = allow;
        return this;
    }

    public PatchRequestBuilder RemoveProperty(string propertyName)
    {
        _properties.Remove(propertyName);
        return this;
    }

    public PatchRequestBuilder ClearProperties()
    {
        _properties.Clear();
        return this;
    }

    public PatchRequestBuilder ClearIdentifiers()
    {
        _identifiers.Clear();
        return this;
    }

    public PatchRequest Build()
    {
        OperationHelpers.ThrowIf(_identifiers.Count == 0 && _keys.Count == 0, "At least one identifier or one key must be specified");
        OperationHelpers.ThrowIf(_properties.Count == 0, "At least one property to update must be specified");
        var identifiersNode = _identifiers.Count == 0 ? null : _identifiers.Count == 1 ? (WhereClause)_identifiers[0] : new GroupClause(GroupOperatorEnum.And, [.._identifiers]);
        return new() {
            Query = identifiersNode,
            Keys = _keys,
            Properties = new(_properties),
            AllowMultiple = _allowMultiple
        };
    }

    public static PatchRequestBuilder New() => new();

    public static PatchRequestBuilder ForId(object id) => new PatchRequestBuilder().WithId(id);

    public static PatchRequestBuilder ForIds<T>(IEnumerable<T> ids) => new PatchRequestBuilder().WithIds(ids);

    public static PatchRequestBuilder ForIdentifier(string propertyName, object value) => new PatchRequestBuilder().WithIdentifier(propertyName, ComparisonOperatorEnum.Equals, value);

    public override string ToString() => $"AllowMultiple={_allowMultiple} Identifiers={_identifiers.Count} Properties={_properties.Count}";
}