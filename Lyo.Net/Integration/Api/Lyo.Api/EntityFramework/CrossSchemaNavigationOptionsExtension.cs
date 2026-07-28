using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Api.EntityFramework;

/// <summary>Carries DI-registered cross-schema navigations on <see cref="Microsoft.EntityFrameworkCore.DbContextOptions" /> for the model customizer.</summary>
public sealed class CrossSchemaNavigationOptionsExtension : IDbContextOptionsExtension
{
    /// <summary>Navigations to apply after <c>OnModelCreating</c>.</summary>
    public IReadOnlyList<CrossSchemaNavigationRegistration> Registrations { get; }

    /// <summary>Creates an extension with the given registrations.</summary>
    public CrossSchemaNavigationOptionsExtension(IReadOnlyList<CrossSchemaNavigationRegistration> registrations)
    {
        Registrations = registrations;
        Info = new ExtensionInfo(this);
    }

    /// <inheritdoc />
    public DbContextOptionsExtensionInfo Info { get; }

    /// <inheritdoc />
    public void ApplyServices(IServiceCollection services) { }

    /// <inheritdoc />
    public void Validate(IDbContextOptions options) { }

    private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        private readonly CrossSchemaNavigationOptionsExtension _extension;

        public override bool IsDatabaseProvider => false;

        public override string LogFragment => "LyoCrossSchemaNavigations ";

        public ExtensionInfo(CrossSchemaNavigationOptionsExtension extension)
            : base(extension)
            => _extension = extension;

        public override int GetServiceProviderHashCode()
        {
            var hash = new HashCode();
            foreach (var r in _extension.Registrations) {
                hash.Add(r.RootEntityType);
                hash.Add(r.RelatedEntityType);
                hash.Add(r.NavigationName);
                hash.Add(r.ForeignKeyPropertyName);
                hash.Add(r.SameContext);
            }

            return hash.ToHashCode();
        }

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is ExtensionInfo o && o._extension.Registrations.Count == _extension.Registrations.Count && o._extension.Registrations.Zip(_extension.Registrations)
                .All(pair => pair.First.RootEntityType == pair.Second.RootEntityType && pair.First.RelatedEntityType == pair.Second.RelatedEntityType &&
                    pair.First.NavigationName == pair.Second.NavigationName && pair.First.ForeignKeyPropertyName == pair.Second.ForeignKeyPropertyName &&
                    pair.First.SameContext == pair.Second.SameContext);

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo) => debugInfo["Lyo:CrossSchemaNavigationCount"] = _extension.Registrations.Count.ToString();
    }
}