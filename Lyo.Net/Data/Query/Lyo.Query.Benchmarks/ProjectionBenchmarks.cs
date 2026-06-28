using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lyo.Api.Services.Crud.Read.Project;
using Lyo.Benchmarking;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Benchmarks;

/// <summary>Benchmarks the projection pipeline: field resolution, SQL projection expression build, and in-memory entity projection.</summary>
[BenchmarkDescription("Projection pipeline over BenchPerson: resolve field specs, build the SQL projection expression, and project entities in memory. Covers a flat selection (Name, Age, IsActive) and a nested-path selection that reaches into the Address object and the Contacts collection (Name, Address.City, Contacts.Kind).")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of generated BenchPerson rows projected in memory (1,000 or 100,000).")]
[BenchmarkDataShape(typeof(BenchPerson), Notes = "Projection selects flat fields plus nested paths (Address.City, Contacts.Kind) out of the full entity.")]
public class ProjectionBenchmarks
{
    private IProjectionService _projection = null!;
    private string[] _fields = null!;
    private string[] _nestedFields = null!;
    private IReadOnlyList<ProjectedFieldSpec> _specs = null!;
    private IReadOnlyList<ProjectedFieldSpec> _nestedSpecs = null!;
    private IReadOnlyList<BenchPerson> _items = null!;

    [Params(1_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _projection = new ProjectionService();
        _fields = [nameof(BenchPerson.Name), nameof(BenchPerson.Age), nameof(BenchPerson.IsActive)];
        _nestedFields = [nameof(BenchPerson.Name), "Address.City", "Contacts.Kind"];
        _specs = _projection.ResolveProjectedFields<BenchPerson>(_fields).Specs;
        _nestedSpecs = _projection.ResolveProjectedFields<BenchPerson>(_nestedFields).Specs;
        _items = QueryBenchmarkSupport.GeneratePeople(RowCount);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Resolve the requested field names into projected field specs (metadata only; baseline).")]
    [BenchmarkSla(MaxMeanUs = 50, Standard = "Resolving a handful of field specs is metadata-only and should be tens of microseconds at most.")]
    public int ResolveProjectedFields()
        => _projection.ResolveProjectedFields<BenchPerson>(_fields).Specs.Count;

    [Benchmark]
    [BenchmarkDescription("Build the SQL projection expression from the resolved specs (no data scan).")]
    [BenchmarkSla(MaxMeanUs = 200, Standard = "Building a projection expression is a build-time step and should stay well under a millisecond.")]
    public bool TryBuildSqlProjectionExpression()
        => _projection.TryBuildSqlProjectionExpression<BenchPerson>(_specs).Projection is not null;

    [Benchmark]
    [BenchmarkDescription("Project the full in-memory entity set down to the selected flat fields and count results.")]
    [BenchmarkSla(MaxMeanMs = 50, Standard = "Projecting up to 100k in-memory rows should complete within tens of milliseconds.")]
    public int ProjectEntities()
        => _projection.ProjectEntities(_items, _specs, QueryIncludeFilterMode.Full, new ProjectedFilterConditions([])).Count;

    [Benchmark]
    [BenchmarkDescription("Project the full in-memory entity set down to nested paths (Address.City, Contacts.Kind) and count results.")]
    [BenchmarkSla(MaxMeanMs = 75, Standard = "Projecting up to 100k in-memory rows through nested paths should complete within tens of milliseconds.")]
    public int ProjectEntities_Nested()
        => _projection.ProjectEntities(_items, _nestedSpecs, QueryIncludeFilterMode.Full, new ProjectedFilterConditions([])).Count;
}
