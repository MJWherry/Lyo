using Lyo.Api.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Api.Tests;

public class CrossSchemaNavigationTests
{
    [Fact]
    public void AddCrossSchema_MapsNavigation_AndExcludesRelatedFromMigrations()
    {
        using var provider = BuildProvider(navs => {
            navs.AddCrossSchema<RootEntity, RelatedEntity>(
                e => e.Related,
                e => e.RelatedId,
                table: "related",
                schema: "other",
                configureRelated: b => {
                    b.HasKey(x => x.Id);
                    b.Property(x => x.Name).HasMaxLength(100);
                });
        });

        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<RootDbContext>>();
        using var context = factory.CreateDbContext();

        var rootType = context.Model.FindEntityType(typeof(RootEntity));
        Assert.NotNull(rootType);
        var nav = rootType.FindNavigation(nameof(RootEntity.Related));
        Assert.NotNull(nav);
        Assert.Equal(typeof(RelatedEntity), nav.TargetEntityType.ClrType);

        var designModel = context.GetService<IDesignTimeModel>().Model;
        var relatedType = designModel.FindEntityType(typeof(RelatedEntity));
        Assert.NotNull(relatedType);
        Assert.Equal("related", relatedType.GetTableName());
        Assert.Equal("other", relatedType.GetSchema());
        Assert.True(relatedType.IsTableExcludedFromMigrations());
    }

    [Fact]
    public void AddSameContext_AddsNavigation_WithoutRemappingRelatedTable()
    {
        using var provider = BuildProvider(navs => {
            navs.AddSameContext<RootEntity, SameContextChild>(
                e => e.Child,
                e => e.ChildId);
        });

        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<RootDbContext>>();
        using var context = factory.CreateDbContext();

        var rootType = context.Model.FindEntityType(typeof(RootEntity))!;
        Assert.NotNull(rootType.FindNavigation(nameof(RootEntity.Child)));

        var childType = context.Model.FindEntityType(typeof(SameContextChild))!;
        Assert.Equal("same_children", childType.GetTableName());
    }

    [Fact]
    public void AddDbContextFactoryWithLyoNavigations_IgnoresPendingModelChangesWarning_WhenRegistrationsExist()
    {
        using var provider = BuildProvider(navs => {
            navs.AddSameContext<RootEntity, SameContextChild>(
                e => e.Child,
                e => e.ChildId);
        });

        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<RootDbContext>>();
        using var context = factory.CreateDbContext();
        var warnings = context.GetService<IDbContextOptions>()
            .FindExtension<CoreOptionsExtension>()
            ?.WarningsConfiguration;
        Assert.NotNull(warnings);
        Assert.Equal(WarningBehavior.Ignore, warnings.GetBehavior(RelationalEventId.PendingModelChangesWarning));
    }

    [Fact]
    public async Task Include_And_Where_OnDiRegisteredNav_KeepCorrectPagingTotals()
    {
        using var provider = BuildProvider(navs => {
            navs.AddSameContext<RootEntity, SameContextChild>(
                e => e.Child,
                e => e.ChildId);
        });

        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<RootDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var ann = new SameContextChild { Id = Guid.NewGuid(), Label = "Ann" };
        var bob = new SameContextChild { Id = Guid.NewGuid(), Label = "Bob" };
        context.Set<SameContextChild>().AddRange(ann, bob);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.Set<RootEntity>().AddRange(
            new RootEntity { Id = Guid.NewGuid(), ChildId = ann.Id },
            new RootEntity { Id = Guid.NewGuid(), ChildId = ann.Id },
            new RootEntity { Id = Guid.NewGuid(), ChildId = bob.Id },
            new RootEntity { Id = Guid.NewGuid(), ChildId = bob.Id },
            new RootEntity { Id = Guid.NewGuid(), ChildId = bob.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var page = await context.Set<RootEntity>()
            .AsNoTracking()
            .Include(r => r.Child)
            .Where(r => r.Child!.Label == "Ann")
            .OrderBy(r => r.Id)
            .Skip(0)
            .Take(10)
            .ToListAsync(TestContext.Current.CancellationToken);

        var total = await context.Set<RootEntity>()
            .AsNoTracking()
            .Where(r => r.Child!.Label == "Ann")
            .CountAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, total);
        Assert.Equal(2, page.Count);
        Assert.All(page, r => Assert.Equal("Ann", r.Child!.Label));
    }

    private static ServiceProvider BuildProvider(Action<CrossSchemaNavigationBuilder<RootDbContext>> configure)
    {
        var services = new ServiceCollection();
        services.AddCrossSchemaNavigations(configure);
        services.AddDbContextFactoryWithLyoNavigations<RootDbContext>(ob => ob.UseSqlite("DataSource=:memory:"));
        return services.BuildServiceProvider();
    }

    private sealed class RootDbContext : DbContext
    {
        public RootDbContext(DbContextOptions<RootDbContext> options)
            : base(options) { }

        public DbSet<RootEntity> Roots => Set<RootEntity>();

        public DbSet<SameContextChild> SameChildren => Set<SameContextChild>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RootEntity>(e => {
                e.ToTable("roots");
                e.HasKey(x => x.Id);
                e.Property(x => x.RelatedId);
                e.Property(x => x.ChildId);
                // Avoid convention FK on RelatedId unless AddCrossSchema reintroduces the nav.
                e.Ignore(x => x.Related);
            });
            modelBuilder.Entity<SameContextChild>(e => {
                e.ToTable("same_children");
                e.HasKey(x => x.Id);
                e.Property(x => x.Label).HasMaxLength(50);
            });
        }
    }

    private sealed class RootEntity
    {
        public Guid Id { get; set; }

        public Guid RelatedId { get; set; }

        public Guid? ChildId { get; set; }

        public RelatedEntity? Related { get; set; }

        public SameContextChild? Child { get; set; }
    }

    private sealed class RelatedEntity
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = "";
    }

    private sealed class SameContextChild
    {
        public Guid Id { get; set; }

        public string Label { get; set; } = "";
    }
}
