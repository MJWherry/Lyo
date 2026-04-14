using System.Linq.Expressions;

namespace Lyo.Api.Services.Crud.Read.Project;

/// <summary>Result of building a SQL-translatable LINQ projection for QueryProject.</summary>
/// <param name="Projection">Expression passed to <c>Queryable.Select</c>, or <c>null</c> when translation is not possible.</param>
/// <param name="ConversionPlan">How raw rows map back to per-<see cref="ProjectedFieldSpec" /> dictionary keys; <c>null</c> when <paramref name="Projection" /> is <c>null</c>.</param>
public sealed record SqlProjectionBuildResult<TDbModel>(Expression<Func<TDbModel, object?>>? Projection, SqlProjectionConversionPlan? ConversionPlan) where TDbModel : class;

/// <summary>Describes tuple / anonymous outer shape after merging sibling collection navigations for SQL.</summary>
public sealed record SqlProjectionConversionPlan(IReadOnlyList<SqlProjectionSlot> Slots);

/// <summary>One outer projection slot (one position in the anonymous tuple returned by EF).</summary>
public abstract record SqlProjectionSlot;

/// <summary>Maps one slot to one <see cref="ProjectedFieldSpec"/> index.</summary>
public sealed record SqlProjectionSingleSlot(int SpecIndex) : SqlProjectionSlot;

/// <summary>
/// One slot is <c>IEnumerable&lt;T&gt;</c> of an anonymous row with V0..Vn-1; expands to parallel arrays for each listed spec index (sibling collection columns).
/// </summary>
public sealed record SqlProjectionMergedCollectionSlot(IReadOnlyList<int> SpecIndicesInOrder) : SqlProjectionSlot;
