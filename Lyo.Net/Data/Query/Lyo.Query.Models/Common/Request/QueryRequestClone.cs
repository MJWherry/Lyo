using Lyo.Exceptions;
using Lyo.Query.Models.Common;

namespace Lyo.Query.Models.Common.Request;

/// <summary>Structural clones of query request DTOs (new list containers; <see cref="WhereClause" /> shared by reference).</summary>
public static class QueryRequestClone
{
    public static QueryConcreteReq Clone(QueryConcreteReq source)
    {
        ArgumentHelpers.ThrowIfNull(source);
        var sourceOptions = source.Options;
        return new() {
            Start = source.Start,
            Amount = source.Amount,
            Options = new() { TotalCountMode = sourceOptions.TotalCountMode, IncludeFilterMode = sourceOptions.IncludeFilterMode },
            WhereClause = source.WhereClause,
            Include = [..source.Include],
            Keys = CloneKeys(source.Keys),
            SortBy = CloneSortBy(source.SortBy)
        };
    }

    public static ProjectionQueryReq Clone(ProjectionQueryReq source)
    {
        ArgumentHelpers.ThrowIfNull(source);
        var sourceOptions = source.Options;
        return new() {
            Start = source.Start,
            Amount = source.Amount,
            Options =
                new() {
                    TotalCountMode = sourceOptions.TotalCountMode,
                    IncludeFilterMode = sourceOptions.IncludeFilterMode,
                    ZipSiblingCollectionSelections = sourceOptions.ZipSiblingCollectionSelections
                },
            WhereClause = source.WhereClause,
            Include = [..source.Include],
            Select = [..source.Select],
            ComputedFields = [..source.ComputedFields.Select(c => new ComputedField(c.Name, c.Template))],
            Keys = CloneKeys(source.Keys),
            SortBy = CloneSortBy(source.SortBy)
        };
    }

    public static QueryReq Clone(QueryReq source)
    {
        ArgumentHelpers.ThrowIfNull(source);
        var sourceOptions = source.Options;
        return new() {
            Start = source.Start,
            Amount = source.Amount,
            Options =
                new() {
                    TotalCountMode = sourceOptions.TotalCountMode,
                    IncludeFilterMode = sourceOptions.IncludeFilterMode,
                    ZipSiblingCollectionSelections = sourceOptions.ZipSiblingCollectionSelections
                },
            WhereClause = source.WhereClause,
            Include = [..source.Include],
            Select = [..source.Select],
            ComputedFields = [..source.ComputedFields.Select(c => new ComputedField(c.Name, c.Template))],
            Keys = CloneKeys(source.Keys),
            SortBy = CloneSortBy(source.SortBy),
            From = CloneFromClause(source.From),
            Joins = [..source.Joins.Select(CloneJoinClause)]
        };
    }

    /// <summary>Clones <paramref name="source" /> preserving its concrete request type.</summary>
    public static QueryRequestBase Clone(QueryRequestBase source)
        => source switch {
            QueryConcreteReq concrete => Clone(concrete),
            ProjectionQueryReq projection => Clone(projection),
            QueryReq root => Clone(root),
            _ => throw new ArgumentException($"Unsupported query request type: {source.GetType().Name}", nameof(source))
        };

    private static List<object[]> CloneKeys(List<object[]> keys) => [..keys.Select(i => i.ToArray())];

    private static List<SortBy> CloneSortBy(List<SortBy> sortBy)
        => [..sortBy.Select(s => new SortBy { PropertyName = s.PropertyName, Direction = s.Direction, Priority = s.Priority })];

    private static FromClause CloneFromClause(FromClause source)
        => new() {
            Alias = source.Alias,
            EntityType = source.EntityType,
            Query = CloneSourceQueryScope(source.Query)
        };

    private static JoinClause CloneJoinClause(JoinClause source)
        => new() {
            Alias = source.Alias,
            EntityType = source.EntityType,
            Query = CloneSourceQueryScope(source.Query),
            Type = source.Type,
            As = source.As,
            On = [..source.On.Select(o => new JoinOn { From = o.From, To = o.To })]
        };

    private static SourceQueryScope? CloneSourceQueryScope(SourceQueryScope? source)
    {
        if (source is null)
            return null;

        return new() {
            WhereClause = source.WhereClause,
            Keys = CloneKeys(source.Keys)
        };
    }
}
