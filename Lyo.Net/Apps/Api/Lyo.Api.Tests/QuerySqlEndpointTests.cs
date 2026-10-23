using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Text.Json;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.ApiEndpoint.Dynamic;
using Lyo.Api.Middleware;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api.Services.Crud.Read.Query.Root;
using Lyo.Common.Core.Enums;
using Lyo.Json;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Tests;

public sealed class QuerySqlEndpointTests(QuerySqlApiFixture fixture) : IClassFixture<QuerySqlApiFixture>
{
    [Fact]
    public async Task QuerySql_ConcreteEquals_ReturnsSqlWithoutQuerying()
    {
        QueryRequestBase request = new QueryConcreteReq { WhereClause = new ConditionClause("Status", ComparisonOperatorEnum.Equals, "Active") };
        var body = await fixture.PostAsync("/api/person/QuerySql", request);
        Assert.Equal(HttpStatusCode.OK, body.Status);
        Assert.Contains("lower(\"status\") = lower($1)", body.Sql, StringComparison.Ordinal);
        Assert.Equal("Active", body.Parameter(0));
        Assert.Equal(0, fixture.QueryCalls);
        Assert.Equal(0, fixture.RootCalls);
    }

    [Fact]
    public async Task QuerySql_ProjectionSelect_ReturnsListedColumnsWithoutQuerying()
    {
        QueryRequestBase request = new ProjectionQueryReq { Select = ["Status", "Name"] };
        var body = await fixture.PostAsync("/api/person/QuerySql", request);
        Assert.Equal(HttpStatusCode.OK, body.Status);
        Assert.Equal("SELECT \"status\", \"name\" FROM \"people\".\"person\"", body.Sql);
        Assert.Equal(0, fixture.QueryCalls);
        Assert.Equal(0, fixture.RootCalls);
    }

    [Fact]
    public async Task QuerySql_RootJoin_ReturnsJoinWithoutQuerying()
    {
        QueryRequestBase request = new QueryReq {
            From = new() { Alias = "o", EntityType = "SqlPreviewOrder" },
            Joins = [
                new() {
                    Alias = "p",
                    EntityType = "SqlPreviewPerson",
                    Type = JoinType.Left,
                    On = [new() { From = "o.PersonId", To = "p.Id" }]
                }
            ],
            Select = ["o.Id", "p.Name"]
        };

        var body = await fixture.PostAsync("/api/QuerySql", request);
        Assert.Equal(HttpStatusCode.OK, body.Status);
        Assert.Contains("LEFT JOIN", body.Sql, StringComparison.Ordinal);
        Assert.Contains("\"o\".\"person_id\" = \"p\".\"id\"", body.Sql, StringComparison.Ordinal);
        Assert.Equal(0, fixture.QueryCalls);
        Assert.Equal(0, fixture.RootCalls);
    }

    [Fact]
    public async Task QuerySql_Regex_ReturnsBadRequest()
    {
        QueryRequestBase request = new QueryConcreteReq { WhereClause = new ConditionClause("Name", ComparisonOperatorEnum.Regex, "a.*") };
        var body = await fixture.PostAsync("/api/person/QuerySql", request);
        Assert.Equal(HttpStatusCode.BadRequest, body.Status);
        Assert.Contains("Regex", body.Raw, StringComparison.Ordinal);
        Assert.Equal(0, fixture.QueryCalls);
    }

    [Fact]
    public async Task QuerySql_ComputedField_ReturnsBadRequest()
    {
        QueryRequestBase request = new ProjectionQueryReq { Select = ["Name"], ComputedFields = [new("label", "{Name}")] };
        var body = await fixture.PostAsync("/api/person/QuerySql", request);
        Assert.Equal(HttpStatusCode.BadRequest, body.Status);
        Assert.Contains("Computed", body.Raw, StringComparison.Ordinal);
        Assert.Equal(0, fixture.QueryCalls);
    }
}

public sealed class QuerySqlApiFixture : IAsyncLifetime
{
    private readonly SpyQueryService _query = new();
    private readonly SpyRootQueryService _root = new();
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public int QueryCalls => _query.Calls;

    public int RootCalls => _root.Calls;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [] });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddDbContextFactory<PreviewDb>(options => options.UseSqlite("Data Source=:memory:"));
        builder.Services.AddSingleton<IQueryService<PreviewDb>>(_query);
        builder.Services.AddSingleton<IRootQueryService<PreviewDb>>(_root);
        builder.Services.ConfigureHttpJsonOptions(options => LyoJsonSerializerOptions.ApplyTo(options.SerializerOptions));
        _app = builder.Build();
        _app.UseMiddleware<LoggingMiddleware>();
        _app.CreateBuilder<PreviewDb, SqlPreviewPerson, SqlPreviewPerson, SqlPreviewPerson, int>("/api/person", "Person").AllowAnonymous().WithQuery().Build();
        _app.MapRootQueryEndpoints<PreviewDb>("api", auth: EndpointAuth.Anonymous());
        await _app.StartAsync();
        _client = new() { BaseAddress = new(_app.Urls.First()) };
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    public async Task<QuerySqlHttpBody> PostAsync(string path, QueryRequestBase request)
    {
        var json = JsonSerializer.Serialize(request, LyoJsonSerializerOptions.Create());
        using var content = new StringContent(json, Encoding.UTF8);
        content.Headers.ContentType = new("application/json");
        using var response = await _client.PostAsync(path, content, TestCt);
        var raw = await response.Content.ReadAsStringAsync(TestCt);
        string? sql = null;
        string? parameter = null;
        if (response.StatusCode == HttpStatusCode.OK) {
            using var document = JsonDocument.Parse(raw);
            sql = document.RootElement.GetProperty("sql").GetString();
            if (document.RootElement.GetProperty("parameters").GetArrayLength() > 0)
                parameter = document.RootElement.GetProperty("parameters")[0].GetProperty("value").GetString();
        }

        return new(response.StatusCode, sql, parameter, raw);
    }

    private sealed class SpyQueryService : IQueryService<PreviewDb>
    {
        public int Calls { get; private set; }

        public Task<QueryRes<TResult>> Query<TDbModel, TResult>(
            QueryConcreteReq queryRequest,
            Expression<Func<TDbModel, object?>> defaultOrder,
            SortDirection defaultSortDirection = SortDirection.Desc,
            CancellationToken ct = default)
            where TDbModel : class
        {
            Calls++;
            throw new InvalidOperationException("Query must not run.");
        }

        public Task<QueryRes<TDbModel>> Query<TDbModel>(
            QueryConcreteReq queryRequest,
            Expression<Func<TDbModel, object?>> defaultOrder,
            SortDirection defaultSortDirection = SortDirection.Desc,
            CancellationToken ct = default)
            where TDbModel : class
        {
            Calls++;
            throw new InvalidOperationException("Query must not run.");
        }

        public Task<ProjectedQueryRes<object?>> QueryProjected<TDbModel>(
            ProjectionQueryReq queryRequest,
            Expression<Func<TDbModel, object?>> defaultOrder,
            SortDirection defaultSortDirection = SortDirection.Desc,
            CancellationToken ct = default)
            where TDbModel : class
        {
            Calls++;
            throw new InvalidOperationException("Query must not run.");
        }

        public Task<TResult?> Get<TDbModel, TResult>(
            object[] keys,
            IEnumerable<string>? includes = null,
            Action<GetContext<TDbModel, PreviewDb>>? before = null,
            Action<GetContext<TDbModel, PreviewDb>>? after = null,
            CancellationToken ct = default)
            where TDbModel : class
        {
            Calls++;
            throw new InvalidOperationException("Query must not run.");
        }

        public Task<TDbModel?> Get<TDbModel>(object[] keys, IEnumerable<string>? includes = null, CancellationToken ct = default)
            where TDbModel : class
        {
            Calls++;
            throw new InvalidOperationException("Query must not run.");
        }
    }

    private sealed class SpyRootQueryService : IRootQueryService<PreviewDb>
    {
        public int Calls { get; private set; }

        public Task<ProjectedQueryRes<object?>> QueryAsync(QueryReq request, RootQueryEntityRegistry registry, CancellationToken ct = default)
        {
            Calls++;
            throw new InvalidOperationException("Root query must not run.");
        }
    }
}

public sealed record QuerySqlHttpBody(HttpStatusCode Status, string? Sql, string? FirstParameter, string Raw)
{
    public string Parameter(int index) => index == 0 ? FirstParameter! : throw new IndexOutOfRangeException();
}

public sealed class SqlPreviewPerson
{
    public int Id { get; set; }

    public string Status { get; set; } = "";

    public string Name { get; set; } = "";

    public int Amount { get; set; }
}

public sealed class SqlPreviewOrder
{
    public int Id { get; set; }

    public int PersonId { get; set; }
}

public sealed class PreviewDb(DbContextOptions<PreviewDb> options)
    : DbContext(options)
{
    public DbSet<SqlPreviewPerson> People => Set<SqlPreviewPerson>();

    public DbSet<SqlPreviewOrder> Orders => Set<SqlPreviewOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SqlPreviewPerson>(entity => {
            entity.ToTable("person", "people");
            entity.HasKey(person => person.Id);
            entity.Property(person => person.Id).HasColumnName("id");
            entity.Property(person => person.Status).HasColumnName("status");
            entity.Property(person => person.Name).HasColumnName("name");
            entity.Property(person => person.Amount).HasColumnName("amount");
        });

        modelBuilder.Entity<SqlPreviewOrder>(entity => {
            entity.ToTable("order", "sales");
            entity.HasKey(order => order.Id);
            entity.Property(order => order.Id).HasColumnName("id");
            entity.Property(order => order.PersonId).HasColumnName("person_id");
        });
    }
}