using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lyo.Api.Services.Crud.Read.Query.Root;

/// <summary>Per-context map of allowlisted entity type names to CLR/EF metadata (built at endpoint map time).</summary>
public sealed class RootQueryEntityRegistry
{
    private readonly Dictionary<string, RootQueryEntityEntry> _byName;

    public IReadOnlyCollection<string> EntityTypeNames => _byName.Keys;

    public RootQueryEntityRegistry(IReadOnlyDictionary<string, RootQueryEntityEntry> byName) => _byName = new(byName, StringComparer.OrdinalIgnoreCase);

    public bool TryGet(string entityTypeName, out RootQueryEntityEntry entry) => _byName.TryGetValue(entityTypeName, out entry!);

    public static RootQueryEntityRegistry FromDbContext<TContext>(TContext context, IEnumerable<Type> allowlistedEntityTypes)
        where TContext : DbContext
    {
        var map = new Dictionary<string, RootQueryEntityEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var clr in allowlistedEntityTypes.Distinct()) {
            var ef = context.Model.FindEntityType(clr);
            if (ef is null)
                continue;

            var props = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in clr.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                if (p.GetIndexParameters().Length == 0)
                    props[p.Name] = p;
            }

            var pk = ef.FindPrimaryKey();
            var entry = new RootQueryEntityEntry(clr, ef, props, pk);
            map[clr.Name] = entry;

            // Accept short names used in clients (Person ↔ PersonEntity).
            var shortName = StripEntitySuffix(clr.Name);
            if (!string.IsNullOrEmpty(shortName) && !map.ContainsKey(shortName))
                map[shortName] = entry;
        }

        return new(map);
    }

    private static string StripEntitySuffix(string clrName)
        => clrName.EndsWith("Entity", StringComparison.Ordinal) && clrName.Length > "Entity".Length ? clrName[..^"Entity".Length] : "";
}

/// <summary>One allowlisted entity for root Query.</summary>
public sealed record RootQueryEntityEntry(Type ClrType, IEntityType EfEntityType, IReadOnlyDictionary<string, PropertyInfo> Properties, IKey? PrimaryKey)
{
    public bool TryGetProperty(string name, out PropertyInfo property) => Properties.TryGetValue(name, out property!);
}