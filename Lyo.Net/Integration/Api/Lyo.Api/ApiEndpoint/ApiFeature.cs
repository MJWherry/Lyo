using System.Collections;

namespace Lyo.Api.ApiEndpoint;

/// <summary>Identifies a CRUD endpoint or pipeline behavior registered for an API group. Extensible via addon packages (self-registering subclasses).</summary>
public abstract record ApiFeature
{
    private static readonly Dictionary<string, ApiFeature> _byName = new(StringComparer.OrdinalIgnoreCase);

    public static readonly ApiFeature Query = new BuiltInApiFeature("Query");
    public static readonly ApiFeature Get = new BuiltInApiFeature("Get");
    public static readonly ApiFeature Create = new BuiltInApiFeature("Create");
    public static readonly ApiFeature CreateBulk = new BuiltInApiFeature("CreateBulk");
    public static readonly ApiFeature Update = new BuiltInApiFeature("Update");
    public static readonly ApiFeature UpdateBulk = new BuiltInApiFeature("UpdateBulk");
    public static readonly ApiFeature Patch = new BuiltInApiFeature("Patch");
    public static readonly ApiFeature PatchBulk = new BuiltInApiFeature("PatchBulk");
    public static readonly ApiFeature Delete = new BuiltInApiFeature("Delete");
    public static readonly ApiFeature DeleteBulk = new BuiltInApiFeature("DeleteBulk");
    public static readonly ApiFeature Upsert = new BuiltInApiFeature("Upsert");
    public static readonly ApiFeature UpsertBulk = new BuiltInApiFeature("UpsertBulk");
    public static readonly ApiFeature UpsertInheritCreate = new BuiltInApiFeature("UpsertInheritCreate");
    public static readonly ApiFeature UpsertInheritUpdate = new BuiltInApiFeature("UpsertInheritUpdate");
    public static readonly ApiFeature PatchInheritsUpdate = new BuiltInApiFeature("PatchInheritsUpdate");
    public static readonly ApiFeature Metadata = new BuiltInApiFeature("Metadata");
    public static readonly ApiFeature ProjectionComputedFields = new BuiltInApiFeature("ProjectionComputedFields");

    public string Name { get; }

    public static IReadOnlyCollection<ApiFeature> AllRegistered => _byName.Values.ToArray();

    protected ApiFeature(string name)
    {
        Name = name;
        _byName.TryAdd(name, this);
    }

    public static ApiFeature? TryFromName(string? name) => !string.IsNullOrEmpty(name) && _byName.TryGetValue(name, out var feature) ? feature : null;

    public sealed override string ToString() => Name;

    private sealed record BuiltInApiFeature(string Name)
        : ApiFeature(Name);
}

/// <summary>Set of <see cref="ApiFeature" /> instances enabled for an endpoint group. Use <see cref="Contains" /> or <c>feature is in set</c>.</summary>
public sealed record ApiFeatureSet : IEnumerable<ApiFeature>
{
    private readonly ApiFeature[] _features;

    public static ApiFeatureSet Empty { get; } = new();

    public static ApiFeatureSet ReadOnly => new(ApiFeature.Query, ApiFeature.Get);

    public static ApiFeatureSet BasicCrud => ReadOnly + ApiFeature.Create + ApiFeature.Update + ApiFeature.Patch + ApiFeature.Delete;

    public static ApiFeatureSet FullCrud => BasicCrud + ApiFeature.Upsert;

    public static ApiFeatureSet BulkOperations => new(ApiFeature.CreateBulk, ApiFeature.UpdateBulk, ApiFeature.PatchBulk, ApiFeature.DeleteBulk, ApiFeature.UpsertBulk);

    /// <summary>Standard CRUD + bulk endpoints (no Export, Metadata, or ProjectionComputedFields).</summary>
    public static ApiFeatureSet CoreAll
        => new(
            ApiFeature.Query, ApiFeature.Get, ApiFeature.Create, ApiFeature.CreateBulk, ApiFeature.Update, ApiFeature.UpdateBulk, ApiFeature.Patch, ApiFeature.PatchBulk,
            ApiFeature.Delete, ApiFeature.DeleteBulk, ApiFeature.Upsert, ApiFeature.UpsertBulk);

    public static ApiFeatureSet StandardInheritance => new(ApiFeature.UpsertInheritCreate, ApiFeature.UpsertInheritUpdate, ApiFeature.PatchInheritsUpdate);

    /// <summary>Default dynamic/typed CRUD preset: <see cref="CoreAll" /> plus upsert/patch inheritance flags.</summary>
    public static ApiFeatureSet DefaultCrud => CoreAll + StandardInheritance;

    public ApiFeatureSet(IEnumerable<ApiFeature> features) => _features = features.Distinct().ToArray();

    public ApiFeatureSet(params ApiFeature[] features) => _features = features.Distinct().ToArray();

    public IEnumerator<ApiFeature> GetEnumerator() => ((IEnumerable<ApiFeature>)_features).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Contains(ApiFeature feature) => _features.Contains(feature);

    public ApiFeatureSet With(ApiFeature feature) => new([.. _features, feature]);

    public ApiFeatureSet Without(ApiFeature feature) => new(_features.Where(f => f != feature));

    public ApiFeatureSet Without(params ApiFeature[] features)
    {
        if (features.Length == 0)
            return this;

        var remove = features.ToHashSet();
        return new(_features.Where(f => !remove.Contains(f)));
    }

    public ApiFeatureSet WithoutName(string name)
    {
        var feature = ApiFeature.TryFromName(name);
        return feature is null ? this : Without(feature);
    }

    public static ApiFeatureSet operator +(ApiFeatureSet set, ApiFeature feature) => set.With(feature);

    public static ApiFeatureSet operator +(ApiFeatureSet a, ApiFeatureSet b) => new(a._features.Concat(b._features));
}