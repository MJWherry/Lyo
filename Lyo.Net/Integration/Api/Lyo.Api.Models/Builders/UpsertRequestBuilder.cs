using System.Diagnostics;
using Lyo.Api.Models.Common.Request;
using Lyo.Exceptions;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;

namespace Lyo.Api.Models.Builders;

[DebuggerDisplay("{ToString(),nq}")]
public class UpsertRequestBuilder<T>
    where T : class
{
    private readonly List<ConditionClause> _identifiers = [];
    private readonly HashSet<string> _ignoredCompareProperties = [];
    private T? _newData;

    public UpsertRequestBuilder<T> WithData(T data)
    {
        _newData = ArgumentHelpers.ThrowIfNullReturn(data, nameof(data));
        return this;
    }

    public UpsertRequestBuilder<T> WithIdentifier(string propertyName, ComparisonOperatorEnum comparator, object? value, bool overwrite = false)
    {
        if (overwrite)
            _identifiers.RemoveAll(f => string.Equals(f.Field, propertyName, StringComparison.OrdinalIgnoreCase) && f.Comparison == comparator);

        _identifiers.Add(new(propertyName, comparator, value));
        return this;
    }

    public UpsertRequestBuilder<T> WithId(string propertyName, object value, bool overwrite = false) => WithIdentifier(propertyName, ComparisonOperatorEnum.Equals, value, overwrite);

    public UpsertRequestBuilder<T> WithId(object value, bool overwrite = false) => WithId("Id", value, overwrite);

    public UpsertRequestBuilder<T> WithIdentifiers(IEnumerable<ConditionClause> identifiers)
    {
        foreach (var identifier in identifiers)
            _identifiers.Add(identifier);

        return this;
    }

    public UpsertRequestBuilder<T> WithIgnoredProperties(string property, params string[] rest)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(property, nameof(property));
        _ignoredCompareProperties.Add(property);
        foreach (var p in rest)
            _ignoredCompareProperties.Add(p);

        return this;
    }

    public UpsertRequestBuilder<T> RemoveIdentifier(string propertyName)
    {
        _identifiers.RemoveAll(f => string.Equals(f.Field, propertyName, StringComparison.OrdinalIgnoreCase));
        return this;
    }

    public UpsertRequestBuilder<T> ClearIdentifiers()
    {
        _identifiers.Clear();
        return this;
    }

    public UpsertRequest<T> Build()
    {
        OperationHelpers.ThrowIfNull(_newData, "Data must be specified using WithData()");
        OperationHelpers.ThrowIf(_identifiers.Count == 0, "At least one identifier must be specified");
        var identifiersNode = _identifiers.Count == 1 ? (WhereClause)_identifiers[0] : new GroupClause(GroupOperatorEnum.And, [.._identifiers]);
        return new(_newData, identifiersNode) { IgnoredCompareProperties = _ignoredCompareProperties.ToList() };
    }

    public static UpsertRequestBuilder<T> New() => new();

    public static UpsertRequestBuilder<T> ForDataWithId(T data, object id) => new UpsertRequestBuilder<T>().WithData(data).WithId(id);

    public static UpsertRequestBuilder<T> ForDataWithIdentifier(T data, string propertyName, object value)
        => new UpsertRequestBuilder<T>().WithData(data).WithId(propertyName, value);

    public override string ToString() => $"Type={typeof(T).Name} Identifiers={_identifiers.Count} IgnoredProperties={_ignoredCompareProperties.Count}";
}