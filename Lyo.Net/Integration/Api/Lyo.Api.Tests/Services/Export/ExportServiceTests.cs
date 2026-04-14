using System.Linq.Expressions;
using System.Reflection;
using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Enums;
using Lyo.Api.Models.Error;
using Lyo.Api.Services.Crud.Read;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api.Services.Export;
using Lyo.Common.Enums;
using Lyo.Common.Records;
using Lyo.Csv;
using Lyo.Csv.Models;
using Lyo.Formatter;
using Lyo.Query.Models.Common.Request;
using Lyo.Testing;
using Lyo.Xlsx;
using Lyo.Xlsx.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lyo.Api.Tests.Services.Export;

public sealed class ExportServiceTests
{
    private readonly ICsvService _csvService;
    private readonly IFormatterService _formatterService;
    private readonly ILogger<ExportService<TestDbContext>> _logger;
    private readonly IXlsxService _xlsxService;

    public ExportServiceTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _logger = loggerFactory.CreateLogger<ExportService<TestDbContext>>();
        _csvService = new CsvService(loggerFactory.CreateLogger<CsvService>());
        _xlsxService = new XlsxService(loggerFactory.CreateLogger<XlsxService>());
        _formatterService = new FormatterService();
    }

    [Fact]
    public async Task ExportAsync_Csv_WithoutColumns_ExportsAllProperties()
    {
        var items = new[] { new TestExportItem(Guid.NewGuid(), "Alice", "Smith", new(2024, 1, 15)), new TestExportItem(Guid.NewGuid(), "Bob", "Jones", new(2024, 2, 20)) };
        var queryService = new FakeQueryService<TestDbContext, TestExportItem>(items);
        var exportService = new ExportService<TestDbContext>(queryService, _csvService, _xlsxService, new(), _formatterService, _logger);
        var request = new ExportRequest { Query = new() { Start = 0, Amount = 10 }, Format = ExportFormat.Csv, Columns = null };
        var (stream, contentType, fileName) = await exportService.ExportAsync<TestExportItem, TestExportItem>(request, x => x.CreatedAt, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        await using var _ = stream;
        var content = await new StreamReader(stream).ReadToEndAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(FileTypeInfo.Csv.MimeType, contentType);
        Assert.Equal("export.csv", fileName);
        Assert.Contains("Id", content);
        Assert.Contains("FirstName", content);
        Assert.Contains("LastName", content);
        Assert.Contains("Alice", content);
        Assert.Contains("Bob", content);
    }

    [Fact]
    public async Task ExportAsync_Csv_WithPropertyColumns_ExportsSelectedColumns()
    {
        var items = new[] { new TestExportItem(Guid.NewGuid(), "Alice", "Smith", new(2024, 1, 15)) };
        var queryService = new FakeQueryService<TestDbContext, TestExportItem>(items, _formatterService);
        var exportService = new ExportService<TestDbContext>(queryService, _csvService, _xlsxService, new(), _formatterService, _logger);
        var request = new ExportRequest { Query = new() { Start = 0, Amount = 10 }, Format = ExportFormat.Csv, Columns = new() { ["First"] = "FirstName", ["Last"] = "LastName" } };
        var (stream, _, _) = await exportService.ExportAsync<TestExportItem, TestExportItem>(request, x => x.CreatedAt, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        await using var _ = stream;
        var content = await new StreamReader(stream).ReadToEndAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Contains("First", content);
        Assert.Contains("Last", content);
        Assert.Contains("Alice", content);
        Assert.Contains("Smith", content);
    }

    [Fact]
    public async Task ExportAsync_Csv_WithFormatterTemplates_FormatsCells()
    {
        var items = new[] { new TestExportItem(Guid.NewGuid(), "Alice", "Smith", new(2024, 1, 15)), new TestExportItem(Guid.NewGuid(), "Bob", "Jones", new(2024, 2, 20)) };
        var queryService = new FakeQueryService<TestDbContext, TestExportItem>(items, _formatterService);
        var exportService = new ExportService<TestDbContext>(queryService, _csvService, _xlsxService, new(), _formatterService, _logger);
        var request = new ExportRequest {
            Query = new() { Start = 0, Amount = 10 },
            Format = ExportFormat.Csv,
            Columns = new() { ["Full Name"] = "{FirstName} {LastName}", ["Created"] = "{CreatedAt:yyyy-MM-dd}" }
        };

        var (stream, _, _) = await exportService.ExportAsync<TestExportItem, TestExportItem>(request, x => x.CreatedAt, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        await using var _ = stream;
        var content = await new StreamReader(stream).ReadToEndAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Contains("Full Name", content);
        Assert.Contains("Created", content);
        Assert.Contains("Alice Smith", content);
        Assert.Contains("Bob Jones", content);
        Assert.Contains("2024-01-15", content);
        Assert.Contains("2024-02-20", content);
    }

    [Fact]
    public async Task ExportAsync_Xlsx_WithFormatterTemplates_FormatsCells()
    {
        var items = new[] { new TestExportItem(Guid.NewGuid(), "Alice", "Smith", new(2024, 1, 15)) };
        var queryService = new FakeQueryService<TestDbContext, TestExportItem>(items, _formatterService);
        var exportService = new ExportService<TestDbContext>(queryService, _csvService, _xlsxService, new(), _formatterService, _logger);
        var request = new ExportRequest {
            Query = new() { Start = 0, Amount = 10 }, Format = ExportFormat.Xlsx, Columns = new() { ["Full Name"] = "{FirstName} {LastName}", ["Date"] = "{CreatedAt:yyyy-MM-dd}" }
        };

        var (stream, contentType, fileName) = await exportService.ExportAsync<TestExportItem, TestExportItem>(request, x => x.CreatedAt, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        await using var _ = stream;
        Assert.Equal(FileTypeInfo.Xlsx.MimeType, contentType);
        Assert.Equal("export.xlsx", fileName);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public async Task ExportAsync_Json_ReturnsJsonArray()
    {
        var items = new[] { new TestExportItem(Guid.NewGuid(), "Alice", "Smith", new(2024, 1, 15)) };
        var queryService = new FakeQueryService<TestDbContext, TestExportItem>(items);
        var exportService = new ExportService<TestDbContext>(queryService, _csvService, _xlsxService, new(), null, _logger);
        var request = new ExportRequest { Query = new() { Start = 0, Amount = 10 }, Format = ExportFormat.Json, Columns = null };
        var (stream, contentType, fileName) = await exportService.ExportAsync<TestExportItem, TestExportItem>(request, x => x.CreatedAt, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        await using var _ = stream;
        var content = await new StreamReader(stream).ReadToEndAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(FileTypeInfo.Json.MimeType, contentType);
        Assert.Equal("export.json", fileName);
        Assert.Contains("Alice", content);
        Assert.Contains("Smith", content);
    }

    [Fact]
    public async Task ExportAsync_WithoutFormatter_WithPropertyColumns_StillWorks()
    {
        var items = new[] { new TestExportItem(Guid.NewGuid(), "Alice", "Smith", new(2024, 1, 15)) };
        var queryService = new FakeQueryService<TestDbContext, TestExportItem>(items);
        var exportService = new ExportService<TestDbContext>(queryService, _csvService, _xlsxService, new(), null, _logger);
        var request = new ExportRequest { Query = new() { Start = 0, Amount = 10 }, Format = ExportFormat.Csv, Columns = new() { ["First"] = "FirstName", ["Last"] = "LastName" } };
        var (stream, _, _) = await exportService.ExportAsync<TestExportItem, TestExportItem>(request, x => x.CreatedAt, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        await using var _ = stream;
        var content = await new StreamReader(stream).ReadToEndAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Contains("First", content);
        Assert.Contains("Last", content);
        Assert.Contains("Alice", content);
        Assert.Contains("Smith", content);
    }

    [Fact]
    public async Task ExportAsync_QueryFailure_Throws()
    {
        var queryService = new FailingQueryService<TestDbContext>();
        var exportService = new ExportService<TestDbContext>(queryService, _csvService, _xlsxService, new(), _formatterService, _logger);
        var request = new ExportRequest { Query = new() { Start = 0, Amount = 10 }, Format = ExportFormat.Csv };
        await Assert.ThrowsAsync<ApiErrorException>(() => exportService.ExportAsync<TestExportItem, TestExportItem>(request, x => x.CreatedAt, ct: TestContext.Current.CancellationToken)).ConfigureAwait(false);
    }

    private sealed record TestExportItem(Guid Id, string FirstName, string LastName, DateTime CreatedAt);

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options)
        : DbContext(options);

    /// <summary>
    /// Fake query service that simulates both typed and projected query paths. QueryProjected builds dictionary rows from the items using reflection, then applies computed
    /// fields via IFormatterService.
    /// </summary>
    private sealed class FakeQueryService<TContext, T>(IReadOnlyList<T> items, IFormatterService? formatter = null) : IQueryService<TContext>
        where TContext : DbContext where T : class
    {
        public Task<QueryRes<TResult>> Query<TDbModel, TResult>(
            QueryReq queryRequest,
            Expression<Func<TDbModel, object?>> defaultOrder,
            SortDirection defaultSortDirection = SortDirection.Desc,
            CancellationToken ct = default)
            where TDbModel : class
            => Task.FromResult(ResultFactory.QuerySuccess(queryRequest, items.Cast<TResult>().ToList(), 0, items.Count, items.Count));

        public Task<QueryRes<TDbModel>> Query<TDbModel>(
            QueryReq queryRequest,
            Expression<Func<TDbModel, object?>> defaultOrder,
            SortDirection defaultSortDirection = SortDirection.Desc,
            CancellationToken ct = default)
            where TDbModel : class
            => Task.FromResult(ResultFactory.QuerySuccess(queryRequest, items.Cast<TDbModel>().ToList(), 0, items.Count, items.Count));

        public Task<ProjectedQueryRes<object?>> QueryProjected<TDbModel>(
            ProjectionQueryReq queryRequest,
            Expression<Func<TDbModel, object?>> defaultOrder,
            SortDirection defaultSortDirection = SortDirection.Desc,
            CancellationToken ct = default)
            where TDbModel : class
        {
            var selectFields = queryRequest.Select;
            var computedFields = queryRequest.ComputedFields;
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead).ToArray();
            var projected = new List<object?>(items.Count);
            foreach (var item in items) {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var field in selectFields) {
                    var prop = props.FirstOrDefault(p => string.Equals(p.Name, field, StringComparison.OrdinalIgnoreCase));
                    if (prop != null)
                        row[field] = prop.GetValue(item);
                }

                foreach (var cf in computedFields) {
                    if (string.IsNullOrWhiteSpace(cf.Name) || string.IsNullOrWhiteSpace(cf.Template) || formatter is null)
                        continue;

                    try {
                        row[cf.Name] = formatter.Format(cf.Template, row);
                    }
                    catch {
                        row[cf.Name] = string.Empty;
                    }
                }

                projected.Add(row);
            }

            return Task.FromResult(ResultFactory.ProjectedQuerySuccess(queryRequest, projected, 0, projected.Count, projected.Count));
        }

        public Task<TResult?> Get<TDbModel, TResult>(
            object[] keys,
            IEnumerable<string>? includes = null,
            Action<GetContext<TDbModel, TContext>>? before = null,
            Action<GetContext<TDbModel, TContext>>? after = null,
            CancellationToken ct = default)
            where TDbModel : class
            => Task.FromResult<TResult?>(default);

        public Task<TDbModel?> Get<TDbModel>(object[] keys, IEnumerable<string>? includes = null, CancellationToken ct = default)
            where TDbModel : class
            => Task.FromResult<TDbModel?>(null);
    }

    private sealed class FailingQueryService<TContext> : IQueryService<TContext>
        where TContext : DbContext
    {
        public Task<QueryRes<TResult>> Query<TDbModel, TResult>(
            QueryReq queryRequest,
            Expression<Func<TDbModel, object?>> defaultOrder,
            SortDirection defaultSortDirection = SortDirection.Desc,
            CancellationToken ct = default)
            where TDbModel : class
            => Task.FromResult(ResultFactory.QueryFailure<TResult>(
                queryRequest,
                LyoProblemDetails.FromCode(Models.Constants.ApiErrorCodes.InvalidQuery, "Test failure", DateTime.UtcNow)));

        public Task<QueryRes<TDbModel>> Query<TDbModel>(
            QueryReq queryRequest,
            Expression<Func<TDbModel, object?>> defaultOrder,
            SortDirection defaultSortDirection = SortDirection.Desc,
            CancellationToken ct = default)
            where TDbModel : class
            => Task.FromResult(ResultFactory.QueryFailure<TDbModel>(
                queryRequest,
                LyoProblemDetails.FromCode(Models.Constants.ApiErrorCodes.InvalidQuery, "Test failure", DateTime.UtcNow)));

        public Task<ProjectedQueryRes<object?>> QueryProjected<TDbModel>(
            ProjectionQueryReq queryRequest,
            Expression<Func<TDbModel, object?>> defaultOrder,
            SortDirection defaultSortDirection = SortDirection.Desc,
            CancellationToken ct = default)
            where TDbModel : class
            => Task.FromResult(ResultFactory.ProjectedQueryFailure<object?>(
                queryRequest,
                LyoProblemDetails.FromCode(Models.Constants.ApiErrorCodes.InvalidQuery, "Test failure", DateTime.UtcNow)));

        public Task<TResult?> Get<TDbModel, TResult>(
            object[] keys,
            IEnumerable<string>? includes = null,
            Action<GetContext<TDbModel, TContext>>? before = null,
            Action<GetContext<TDbModel, TContext>>? after = null,
            CancellationToken ct = default)
            where TDbModel : class
            => Task.FromResult<TResult?>(default);

        public Task<TDbModel?> Get<TDbModel>(object[] keys, IEnumerable<string>? includes = null, CancellationToken ct = default)
            where TDbModel : class
            => Task.FromResult<TDbModel?>(null);
    }
}