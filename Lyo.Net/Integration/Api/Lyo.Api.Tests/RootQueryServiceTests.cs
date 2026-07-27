using System.Collections;
using Lyo.Api.Services.Crud.Read;
using Lyo.Api.Services.Crud.Read.Query.Root;
using Lyo.Cache;
using Lyo.Formatter;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Api.Tests;

/// <summary>SQLite-backed execution tests for root <c>/Query</c> (joins, left-miss, fan-out collapse, From-side paging).</summary>
public sealed class RootQueryServiceTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _services;
    private readonly RootQueryEntityRegistry _registry;
    private readonly IRootQueryService<RootQueryTestDbContext> _service;

    public RootQueryServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLocalCache(o => o.Enabled = false);
        services.AddLyoQueryServices();
        services.AddFormatterService();
        services.AddSingleton(new QueryOptions { DefaultPageSize = 100, MaxPageSize = 2000 });
        services.AddDbContextFactory<RootQueryTestDbContext>(o => o.UseSqlite(_connection));
        services.AddScoped<IRootQueryService<RootQueryTestDbContext>, RootQueryService<RootQueryTestDbContext>>();
        services.AddSingleton(NullLogger<RootQueryService<RootQueryTestDbContext>>.Instance);
        _services = services.BuildServiceProvider();

        using (var ctx = _services.GetRequiredService<IDbContextFactory<RootQueryTestDbContext>>().CreateDbContext()) {
            ctx.Database.EnsureCreated();
            Seed(ctx);
        }

        using (var ctx = _services.GetRequiredService<IDbContextFactory<RootQueryTestDbContext>>().CreateDbContext())
            _registry = RootQueryEntityRegistry.FromDbContext(
                ctx,
                [typeof(PersonEntity), typeof(ContactAddressEntity), typeof(AddressEntity)]);

        _service = _services.GetRequiredService<IRootQueryService<RootQueryTestDbContext>>();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task LeftJoin_KeepsPeopleWithoutContactAddress()
    {
        var req = BasePersonContactAddressQuery(JoinType.Left)
            .SetPagination(0, 50)
            .SetTotalCountMode(QueryTotalCountMode.Exact)
            .Build();

        var res = await _service.QueryAsync(req, _registry, Ct);

        Assert.True(res.IsSuccess, res.Error?.Detail);
        Assert.Equal(3, res.Items!.Count);
        Assert.Equal(3, res.Total);

        var lonely = FindByFirstName(res.Items, "Lonely");
        Assert.NotNull(lonely);
        Assert.Empty(GetJoinList(lonely, "c"));
    }

    [Fact]
    public async Task InnerJoin_ExcludesPeopleWithoutMatchingJoinRows()
    {
        // Both joins Inner: Lonely (no contact) and Single (contact, no address) drop out.
        var req = BasePersonContactAddressQuery(JoinType.Inner)
            .SetPagination(0, 50)
            .Build();

        var res = await _service.QueryAsync(req, _registry, Ct);

        Assert.True(res.IsSuccess, res.Error?.Detail);
        var only = Assert.Single(res.Items!);
        Assert.Equal("Multi", GetString(AsDict(only), "FirstName"));
    }

    [Fact]
    public async Task InnerThenLeft_KeepsContactWithoutAddress_DropsPersonWithoutContact()
    {
        var req = QueryReqBuilder.New()
            .From("p", nameof(PersonEntity))
            .Join(
                "c", nameof(ContactAddressEntity), JoinType.Inner, on => {
                    on.Add(new JoinOn { From = "p.Id", To = "c.PersonId" });
                }, "c")
            .Join(
                "a", nameof(AddressEntity), JoinType.Left, on => {
                    on.Add(new JoinOn { From = "c.AddressId", To = "a.Id" });
                }, "a")
            .AddSelects("p.FirstName", "c.StartDate", "a.StreetName")
            .SetPagination(0, 50)
            .Build();

        var res = await _service.QueryAsync(req, _registry, Ct);

        Assert.True(res.IsSuccess, res.Error?.Detail);
        Assert.Equal(2, res.Items!.Count);
        Assert.DoesNotContain(res.Items, i => string.Equals(GetString(AsDict(i), "FirstName"), "Lonely", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(FindByFirstName(res.Items, "Single"));
    }

    [Fact]
    public async Task ComputedField_FormatsFromAndJoinPlaceholders_MustacheAndSmartFormat()
    {
        var req = BasePersonContactAddressQuery(JoinType.Left)
            .SetPagination(0, 50)
            .AddComputedField("oo", "{{p.firstname}}{{c.startdate}}")
            .Build();

        var res = await _service.QueryAsync(req, _registry, Ct);

        Assert.True(res.IsSuccess, res.Error?.Detail);
        var multi = FindByFirstName(res.Items!, "Multi");
        Assert.Null(GetValue(multi, "oo"));
        Assert.Null(GetValue(multi, "c.oo"));

        var contacts = GetJoinList(multi, "c");
        Assert.Equal(2, contacts.Count);
        var values = contacts.Select(c => GetString(AsDict(c), "oo") ?? "").ToList();
        Assert.All(values, v => Assert.StartsWith("Multi", v, StringComparison.Ordinal));
        // DateTimeOffset default format varies by culture; assert month/day fragments from seeded t0/t1.
        Assert.Contains(values, v => v.Contains("1/1/2024", StringComparison.Ordinal) || v.Contains("2024-01-01", StringComparison.Ordinal));
        Assert.Contains(values, v => v.Contains("2/1/2024", StringComparison.Ordinal) || v.Contains("2024-02-01", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ComputedField_FromSideOnly_AddsScalarColumn()
    {
        var req = QueryReqBuilder.New()
            .From("p", nameof(PersonEntity))
            .AddSelects("p.FirstName")
            .AddComputedField("label", "Hi {p.FirstName}")
            .SetPagination(0, 50)
            .Build();

        var res = await _service.QueryAsync(req, _registry, Ct);

        Assert.True(res.IsSuccess, res.Error?.Detail);
        var lonely = FindByFirstName(res.Items!, "Lonely");
        Assert.Equal("Hi Lonely", GetString(lonely, "label"));
        Assert.Null(GetValue(lonely, "c"));
    }

    [Fact]
    public async Task ComputedField_DeepestJoinAlias_LandsOnNestedAddressBagsOnly()
    {
        var req = BasePersonContactAddressQuery(JoinType.Left)
            .SetPagination(0, 50)
            .AddComputedField("oo", "{p.FirstName}{a.StreetName}")
            .Build();

        var res = await _service.QueryAsync(req, _registry, Ct);

        Assert.True(res.IsSuccess, res.Error?.Detail);
        var multi = FindByFirstName(res.Items!, "Multi");
        Assert.Null(GetValue(multi, "oo"));
        Assert.Null(GetValue(multi, "a.oo"));
        Assert.Null(GetValue(multi, "c.oo"));

        var contacts = GetJoinList(multi, "c");
        Assert.Equal(2, contacts.Count);
        foreach (var c in contacts) {
            var contactBag = AsDict(c);
            Assert.Null(GetValue(contactBag, "oo"));
            var address = GetNestedBag(contactBag, "a");
            Assert.NotNull(address);
            var oo = GetString(address, "oo");
            Assert.NotNull(oo);
            Assert.StartsWith("Multi", oo, StringComparison.Ordinal);
            Assert.True(
                oo.Contains("Oak", StringComparison.OrdinalIgnoreCase) || oo.Contains("Pine", StringComparison.OrdinalIgnoreCase),
                $"Expected street in oo, got '{oo}'");
        }
    }

    [Fact]
    public async Task FanOut_CollapsesToOneItemPerPerson_WithNestedBags()
    {
        var req = BasePersonContactAddressQuery(JoinType.Left)
            .SetPagination(0, 50)
            .Build();

        var res = await _service.QueryAsync(req, _registry, Ct);
        Assert.True(res.IsSuccess, res.Error?.Detail);
        Assert.Equal(3, res.Items!.Count);
        var multi = FindByFirstName(res.Items, "Multi");
        var contacts = GetJoinList(multi, "c");
        Assert.Equal(2, contacts.Count);

        var streets = contacts
            .Select(c => GetNestedBag(AsDict(c), "a"))
            .Where(a => a != null)
            .Select(a => GetString(a!, "StreetName"))
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();
        Assert.Equal(["Oak St", "Pine St"], streets);
    }

    [Fact]
    public async Task Paging_IsFromSide_ItemCountAtMostAmount()
    {
        var req = BasePersonContactAddressQuery(JoinType.Left)
            .SetPagination(0, 2)
            .SetTotalCountMode(QueryTotalCountMode.Exact)
            .Build();

        var res = await _service.QueryAsync(req, _registry, Ct);

        Assert.True(res.IsSuccess, res.Error?.Detail);
        Assert.Equal(2, res.Items!.Count);
        Assert.Equal(3, res.Total);
        Assert.True(res.HasMore);
        // Multi has 2 contacts; without collapse this page would exceed amount.
        Assert.True(res.Items.Count <= req.Amount);
    }

    [Fact]
    public async Task Paging_StartSkipsFromRows()
    {
        var page0 = await _service.QueryAsync(
            BasePersonContactAddressQuery(JoinType.Left).SetPagination(0, 1).AddSort("p.FirstName", Common.Enums.SortDirection.Asc).Build(),
            _registry,
            Ct);
        var page1 = await _service.QueryAsync(
            BasePersonContactAddressQuery(JoinType.Left).SetPagination(1, 1).AddSort("p.FirstName", Common.Enums.SortDirection.Asc).Build(),
            _registry,
            Ct);

        Assert.True(page0.IsSuccess && page1.IsSuccess);
        var item0 = Assert.Single(page0.Items!);
        var item1 = Assert.Single(page1.Items!);
        Assert.NotEqual(GetString(AsDict(item0), "FirstName"), GetString(AsDict(item1), "FirstName"));
    }

    [Fact]
    public async Task ContactWithoutAddress_StillReturned_UnderLeftJoin()
    {
        var req = BasePersonContactAddressQuery(JoinType.Left)
            .SetPagination(0, 50)
            .Build();

        var res = await _service.QueryAsync(req, _registry, Ct);
        var single = FindByFirstName(res.Items!, "Single");
        var contacts = GetJoinList(single, "c");
        Assert.Single(contacts);

        var bag = AsDict(contacts[0]);
        Assert.NotNull(GetValue(bag, "StartDate"));
        Assert.Null(GetNestedBag(bag, "a"));
    }

    [Fact]
    public async Task NoJoins_ReturnsFlatProjection()
    {
        var req = QueryReqBuilder.New()
            .From("p", nameof(PersonEntity))
            .AddSelects("p.FirstName")
            .SetPagination(0, 10)
            .Build();

        var res = await _service.QueryAsync(req, _registry, Ct);

        Assert.True(res.IsSuccess, res.Error?.Detail);
        Assert.Equal(3, res.Items!.Count);
        Assert.Contains(res.Items, i => (i is string s && s == "Lonely") || (i is IDictionary d && GetString(d, "FirstName") == "Lonely"));
    }

    [Fact]
    public async Task ShortEntityTypeNames_Resolve()
    {
        var req = QueryReqBuilder.New()
            .From("p", "Person")
            .Join("c", "ContactAddress", JoinType.Left, on => on.Add(new JoinOn { From = "p.Id", To = "c.PersonId" }), "c")
            .Join("a", "Address", JoinType.Left, on => on.Add(new JoinOn { From = "c.AddressId", To = "a.Id" }), "a")
            .AddSelects("p.FirstName", "c.StartDate", "a.StreetName")
            .SetPagination(0, 50)
            .Build();

        var res = await _service.QueryAsync(req, _registry, Ct);
        Assert.True(res.IsSuccess, res.Error?.Detail);
        Assert.Equal(3, res.Items!.Count);
    }

    [Fact]
    public async Task OuterWhere_FiltersFromRows()
    {
        var req = BasePersonContactAddressQuery(JoinType.Left)
            .AddWhere(w => w.Equals("p.FirstName", "Multi"))
            .SetPagination(0, 50)
            .SetTotalCountMode(QueryTotalCountMode.Exact)
            .Build();

        var res = await _service.QueryAsync(req, _registry, Ct);

        Assert.True(res.IsSuccess, res.Error?.Detail);
        var only = Assert.Single(res.Items!);
        Assert.Equal("Multi", GetString(AsDict(only), "FirstName"));
        Assert.Equal(1, res.Total);
    }

    [Fact]
    public async Task FromQueryScope_Where_FiltersSourceBeforeJoin()
    {
        var req = QueryReqBuilder.New()
            .From("p", nameof(PersonEntity), scope => {
                scope.WhereClause = WhereClauseBuilder.Condition("FirstName", ComparisonOperatorEnum.Equals, "Lonely");
            })
            .Join(
                "c", nameof(ContactAddressEntity), JoinType.Left, on => {
                    on.Add(new JoinOn { From = "p.Id", To = "c.PersonId" });
                }, "c")
            .AddSelects("p.FirstName", "c.StartDate")
            .SetPagination(0, 50)
            .Build();

        var res = await _service.QueryAsync(req, _registry, Ct);

        Assert.True(res.IsSuccess, res.Error?.Detail);
        var only = Assert.Single(res.Items!);
        Assert.Equal("Lonely", GetString(AsDict(only), "FirstName"));
        Assert.Empty(GetJoinList(AsDict(only), "c"));
    }

    [Fact]
    public async Task JoinQueryScope_Where_FiltersJoinSource()
    {
        var oakStart = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var req = QueryReqBuilder.New()
            .From("p", nameof(PersonEntity))
            .Join(
                "c",
                nameof(ContactAddressEntity),
                JoinType.Left,
                on => on.Add(new JoinOn { From = "p.Id", To = "c.PersonId" }),
                "c",
                scope => {
                    scope.WhereClause = WhereClauseBuilder.Condition("StartDate", ComparisonOperatorEnum.Equals, oakStart);
                })
            .AddSelects("p.FirstName", "c.StartDate")
            .AddWhere(w => w.Equals("p.FirstName", "Multi"))
            .SetPagination(0, 50)
            .Build();

        var res = await _service.QueryAsync(req, _registry, Ct);

        Assert.True(res.IsSuccess, res.Error?.Detail);
        var multi = Assert.Single(res.Items!);
        Assert.Single(GetJoinList(AsDict(multi), "c"));
    }

    [Fact]
    public async Task ValidationFailure_ReturnsProjectedFailure()
    {
        var req = QueryReqBuilder.New().From("p", nameof(PersonEntity)).Build();
        var res = await _service.QueryAsync(req, _registry, Ct);
        Assert.False(res.IsSuccess);
        Assert.NotNull(res.Error);
    }

    private static QueryReqBuilder BasePersonContactAddressQuery(JoinType joinType)
        => QueryReqBuilder.New()
            .From("p", nameof(PersonEntity))
            .Join(
                "c", nameof(ContactAddressEntity), joinType, on => {
                    on.Add(new JoinOn { From = "p.Id", To = "c.PersonId" });
                }, "c")
            .Join(
                "a", nameof(AddressEntity), joinType, on => {
                    on.Add(new JoinOn { From = "c.AddressId", To = "a.Id" });
                }, "a")
            .AddSelects("p.FirstName", "c.StartDate", "c.CreatedTimestamp", "a.StreetName");

    private static void Seed(RootQueryTestDbContext ctx)
    {
        var oak = new AddressEntity { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), StreetName = "Oak St" };
        var pine = new AddressEntity { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), StreetName = "Pine St" };

        var lonely = new PersonEntity { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), FirstName = "Lonely" };
        var multi = new PersonEntity { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"), FirstName = "Multi" };
        var single = new PersonEntity { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3"), FirstName = "Single" };

        var t0 = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var t1 = new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero);

        ctx.Addresses.AddRange(oak, pine);
        ctx.People.AddRange(lonely, multi, single);
        ctx.ContactAddresses.AddRange(
            new ContactAddressEntity {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1"),
                PersonId = multi.Id,
                AddressId = oak.Id,
                StartDate = t0,
                CreatedTimestamp = t0
            },
            new ContactAddressEntity {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2"),
                PersonId = multi.Id,
                AddressId = pine.Id,
                StartDate = t1,
                CreatedTimestamp = t1
            },
            new ContactAddressEntity {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc3"),
                PersonId = single.Id,
                AddressId = null,
                StartDate = t2,
                CreatedTimestamp = t2
            });
        ctx.SaveChanges();
    }

    private static IDictionary FindByFirstName(IReadOnlyList<object?> items, string firstName)
        => items.Select(AsDict).First(d => string.Equals(GetString(d, "FirstName"), firstName, StringComparison.OrdinalIgnoreCase));

    private static List<object?> GetJoinList(IDictionary root, string resultName)
    {
        var value = GetValue(root, resultName);
        return value switch {
            null => [],
            IList list => list.Cast<object?>().ToList(),
            _ => [value]
        };
    }

    private static IDictionary? GetNestedBag(IDictionary parent, string resultName)
    {
        var value = GetValue(parent, resultName);
        return value switch {
            null => null,
            IDictionary d => d,
            IList { Count: 1 } list => AsDict(list[0]),
            _ => null
        };
    }

    private static IDictionary AsDict(object? item)
    {
        Assert.NotNull(item);
        Assert.IsAssignableFrom<IDictionary>(item);
        return (IDictionary)item;
    }

    private static object? GetValue(IDictionary dict, string key)
    {
        foreach (DictionaryEntry e in dict) {
            if (e.Key is string s && string.Equals(s, key, StringComparison.OrdinalIgnoreCase))
                return e.Value;
        }

        return null;
    }

    private static string? GetString(IDictionary dict, string key) => GetValue(dict, key)?.ToString();

    private sealed class PersonEntity
    {
        public Guid Id { get; set; }
        public string? FirstName { get; set; }
    }

    private sealed class ContactAddressEntity
    {
        public Guid Id { get; set; }
        public Guid PersonId { get; set; }
        public Guid? AddressId { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? CreatedTimestamp { get; set; }
    }

    private sealed class AddressEntity
    {
        public Guid Id { get; set; }
        public string? StreetName { get; set; }
    }

    private sealed class RootQueryTestDbContext : DbContext
    {
        public RootQueryTestDbContext(DbContextOptions<RootQueryTestDbContext> options)
            : base(options)
        {
        }

        public DbSet<PersonEntity> People => Set<PersonEntity>();
        public DbSet<ContactAddressEntity> ContactAddresses => Set<ContactAddressEntity>();
        public DbSet<AddressEntity> Addresses => Set<AddressEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PersonEntity>().HasKey(e => e.Id);
            modelBuilder.Entity<ContactAddressEntity>().HasKey(e => e.Id);
            modelBuilder.Entity<AddressEntity>().HasKey(e => e.Id);
        }
    }
}
