using Lyo.Api.Services.Crud.Read.Query.Root;
using Lyo.Common.Enums;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Tests;

public sealed class RootQueryValidatorTests
{
    private sealed class OrderEntity
    {
        public Guid Id { get; set; }
        public Guid PersonId { get; set; }
        public string? Label { get; set; }
    }

    private sealed class PersonEntity
    {
        public Guid Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        public DbSet<OrderEntity> Orders => Set<OrderEntity>();
        public DbSet<PersonEntity> People => Set<PersonEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderEntity>().HasKey(e => e.Id);
            modelBuilder.Entity<PersonEntity>().HasKey(e => e.Id);
        }
    }

    private static RootQueryEntityRegistry CreateRegistry()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite($"DataSource=file:rootquery-{Guid.NewGuid():N}?mode=memory&cache=shared").Options;
        using var ctx = new TestDbContext(options);
        ctx.Database.OpenConnection();
        ctx.Database.EnsureCreated();
        return RootQueryEntityRegistry.FromDbContext(ctx, [typeof(OrderEntity), typeof(PersonEntity)]);
    }

    private static QueryReq ValidBase()
        => QueryReqBuilder.New()
            .From("o", nameof(OrderEntity))
            .Join("p", nameof(PersonEntity), JoinType.Left, on => on.Add(new JoinOn { From = "o.PersonId", To = "p.Id" }), "recipient")
            .AddSelects("o.Id", "p.FirstName")
            .Build();

    [Fact]
    public void Validate_Accepts_ValidLeftJoin()
    {
        var errors = RootQueryValidator.Validate(ValidBase(), CreateRegistry());
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_Accepts_BareFromPropertyInOuterWhereAndSort()
    {
        var registry = CreateRegistry();
        var req = QueryReqBuilder.New()
            .From("o", nameof(OrderEntity))
            .AddSelects("o.Label")
            .AddWhere(w => w.Equals("Label", "x"))
            .AddSort("Label")
            .Build();

        Assert.Empty(RootQueryValidator.Validate(req, registry));
    }

    [Fact]
    public void Validate_Accepts_FromAliasedOuterWhereAndSort()
    {
        var registry = CreateRegistry();
        var req = QueryReqBuilder.New()
            .From("o", nameof(OrderEntity))
            .AddSelects("o.Label")
            .AddWhere(w => w.Equals("o.Label", "x"))
            .AddSort("o.Label")
            .Build();

        Assert.Empty(RootQueryValidator.Validate(req, registry));
    }

    [Fact]
    public void Validate_Rejects_JoinAliasInOuterWhere()
    {
        var registry = CreateRegistry();
        var req = QueryReqBuilder.New()
            .From("o", nameof(OrderEntity))
            .Join("p", nameof(PersonEntity), JoinType.Left, on => on.Add(new JoinOn { From = "o.PersonId", To = "p.Id" }))
            .AddSelects("o.Id", "p.FirstName")
            .AddWhere(w => w.Equals("p.FirstName", "Ann"))
            .Build();

        var errors = RootQueryValidator.Validate(req, registry);
        Assert.Contains(errors, e => e.Description.Contains("From alias", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Rejects_JoinAliasInSort()
    {
        var registry = CreateRegistry();
        var req = ValidBase();
        req.SortBy.Add(new SortBy("p.FirstName", SortDirection.Asc));

        var errors = RootQueryValidator.Validate(req, registry);
        Assert.Contains(errors, e => e.Description.Contains("SortBy", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Accepts_ShortEntityTypeName()
    {
        var registry = CreateRegistry();
        var req = QueryReqBuilder.New()
            .From("o", "Order")
            .Join("p", "Person", JoinType.Left, on => on.Add(new JoinOn { From = "o.PersonId", To = "p.Id" }))
            .AddSelects("o.Id", "p.FirstName")
            .Build();

        Assert.Empty(RootQueryValidator.Validate(req, registry));
    }

    [Fact]
    public void Validate_Rejects_UnknownEntityType()
    {
        var registry = CreateRegistry();
        var req = QueryReqBuilder.New().From("o", "MissingEntity").AddSelects("o.Id").Build();
        var errors = RootQueryValidator.Validate(req, registry);
        Assert.Contains(errors, e => e.Description.Contains("Unknown", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Rejects_Include()
    {
        var registry = CreateRegistry();
        var req = QueryReqBuilder.New().From("o", nameof(OrderEntity)).AddSelects("o.Id").Build();
        req.Include.Add("Person");
        var errors = RootQueryValidator.Validate(req, registry);
        Assert.Contains(errors, e => e.Description.Contains("Include", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Rejects_EmptySelect()
    {
        var registry = CreateRegistry();
        var req = QueryReqBuilder.New().From("o", nameof(OrderEntity)).Build();
        var errors = RootQueryValidator.Validate(req, registry);
        Assert.Contains(errors, e => e.Description.Contains("Select", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Rejects_MissingFromAliasAndEntityType()
    {
        var registry = CreateRegistry();
        var req = new QueryReq { From = new FromClause { Alias = "", EntityType = "" }, Select = ["o.Id"] };
        var errors = RootQueryValidator.Validate(req, registry);
        Assert.Contains(errors, e => e.Description.Contains("From.Alias", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, e => e.Description.Contains("From.EntityType", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Rejects_DuplicateJoinAlias()
    {
        var registry = CreateRegistry();
        var req = QueryReqBuilder.New()
            .From("o", nameof(OrderEntity))
            .Join("p", nameof(PersonEntity), JoinType.Left, on => on.Add(new JoinOn { From = "o.PersonId", To = "p.Id" }))
            .Join("p", nameof(PersonEntity), JoinType.Left, on => on.Add(new JoinOn { From = "o.PersonId", To = "p.Id" }))
            .AddSelects("o.Id")
            .Build();

        var errors = RootQueryValidator.Validate(req, registry);
        Assert.Contains(errors, e => e.Description.Contains("duplicated", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Rejects_JoinAliasMatchingFromAlias()
    {
        var registry = CreateRegistry();
        var req = QueryReqBuilder.New()
            .From("o", nameof(OrderEntity))
            .Join("o", nameof(PersonEntity), JoinType.Left, on => on.Add(new JoinOn { From = "o.PersonId", To = "o.Id" }))
            .AddSelects("o.Id")
            .Build();

        var errors = RootQueryValidator.Validate(req, registry);
        Assert.Contains(errors, e => e.Description.Contains("duplicated", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Rejects_EmptyOn()
    {
        var registry = CreateRegistry();
        var req = QueryReqBuilder.New().From("o", nameof(OrderEntity)).AddSelects("o.Id").Build();
        req.Joins.Add(new JoinClause { Alias = "p", EntityType = nameof(PersonEntity), Type = JoinType.Left, On = [] });

        var errors = RootQueryValidator.Validate(req, registry);
        Assert.Contains(errors, e => e.Description.Contains(".On", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Rejects_UnsupportedJoinType()
    {
        var registry = CreateRegistry();
        var req = QueryReqBuilder.New().From("o", nameof(OrderEntity)).AddSelects("o.Id").Build();
        req.Joins.Add(
            new JoinClause {
                Alias = "p",
                EntityType = nameof(PersonEntity),
                Type = (JoinType)99,
                On = [new JoinOn { From = "o.PersonId", To = "p.Id" }]
            });

        var errors = RootQueryValidator.Validate(req, registry);
        Assert.Contains(errors, e => e.Description.Contains("Inner or Left", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Rejects_MissingJoinAliasAndEntityType()
    {
        var registry = CreateRegistry();
        var req = QueryReqBuilder.New().From("o", nameof(OrderEntity)).AddSelects("o.Id").Build();
        req.Joins.Add(new JoinClause { Alias = "", EntityType = "", Type = JoinType.Left, On = [new JoinOn { From = "o.PersonId", To = "p.Id" }] });

        var errors = RootQueryValidator.Validate(req, registry);
        Assert.Contains(errors, e => e.Description.Contains("Alias is required", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, e => e.Description.Contains("EntityType is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Rejects_UnknownSelectProperty()
    {
        var registry = CreateRegistry();
        var req = QueryReqBuilder.New().From("o", nameof(OrderEntity)).AddSelects("o.NotAField").Build();
        var errors = RootQueryValidator.Validate(req, registry);
        Assert.Contains(errors, e => e.Description.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Rejects_SelectWithoutAliasDot()
    {
        var registry = CreateRegistry();
        var req = QueryReqBuilder.New().From("o", nameof(OrderEntity)).AddSelects("Label").Build();
        var errors = RootQueryValidator.Validate(req, registry);
        Assert.Contains(errors, e => e.Description.Contains("alias.property", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Rejects_UnknownSelectAlias()
    {
        var registry = CreateRegistry();
        var req = QueryReqBuilder.New().From("o", nameof(OrderEntity)).AddSelects("x.Id").Build();
        var errors = RootQueryValidator.Validate(req, registry);
        Assert.Contains(errors, e => e.Description.Contains("unknown alias", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Rejects_InvalidOnPath()
    {
        var registry = CreateRegistry();
        var req = QueryReqBuilder.New()
            .From("o", nameof(OrderEntity))
            .Join("p", nameof(PersonEntity), JoinType.Left, on => on.Add(new JoinOn { From = "o.Missing", To = "p.Id" }))
            .AddSelects("o.Id")
            .Build();

        var errors = RootQueryValidator.Validate(req, registry);
        Assert.Contains(errors, e => e.Description.Contains("On.From", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void QueryCacheKey_Differs_WhenJoinChanges()
    {
        var a = QueryReqBuilder.New()
            .From("o", nameof(OrderEntity))
            .Join("p", nameof(PersonEntity), JoinType.Left, on => on.Add(new JoinOn { From = "o.PersonId", To = "p.Id" }), "recipient")
            .AddSelects("o.Id", "p.FirstName")
            .Build();
        var b = QueryReqBuilder.New()
            .From("o", nameof(OrderEntity))
            .Join("p", nameof(PersonEntity), JoinType.Inner, on => on.Add(new JoinOn { From = "o.PersonId", To = "p.Id" }), "recipient")
            .AddSelects("o.Id", "p.FirstName")
            .Build();

        var keyA = Lyo.Api.Services.Crud.Read.Query.QueryCacheKeyBuilder.BuildRootQuery(a, "TestDbContext");
        var keyB = Lyo.Api.Services.Crud.Read.Query.QueryCacheKeyBuilder.BuildRootQuery(b, "TestDbContext");
        Assert.NotEqual(keyA, keyB);
    }

    [Fact]
    public void QueryCacheKey_Differs_WhenPagingOrSelectChanges()
    {
        var a = QueryReqBuilder.New().From("o", nameof(OrderEntity)).AddSelects("o.Id").SetPagination(0, 10).Build();
        var b = QueryReqBuilder.New().From("o", nameof(OrderEntity)).AddSelects("o.Id").SetPagination(10, 10).Build();
        var c = QueryReqBuilder.New().From("o", nameof(OrderEntity)).AddSelects("o.Label").SetPagination(0, 10).Build();

        var keyA = Lyo.Api.Services.Crud.Read.Query.QueryCacheKeyBuilder.BuildRootQuery(a, "Ctx");
        var keyB = Lyo.Api.Services.Crud.Read.Query.QueryCacheKeyBuilder.BuildRootQuery(b, "Ctx");
        var keyC = Lyo.Api.Services.Crud.Read.Query.QueryCacheKeyBuilder.BuildRootQuery(c, "Ctx");
        Assert.NotEqual(keyA, keyB);
        Assert.NotEqual(keyA, keyC);
    }
}
