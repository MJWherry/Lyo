using BenchmarkDotNet.Attributes;
using Lyo.Benchmarking;
using Lyo.Common.Enums;
using Lyo.Query.Models.Common;
using Lyo.Query.Services.WhereClause;

namespace Lyo.Query.Benchmarks;

/// <summary>In-memory benchmarks for ordering: single-property sort vs multi-key ordering.</summary>
[BenchmarkDescription("Ordering an in-memory BenchPerson list: single-property sort vs two-key and three-key composite ordering with a tie-break key.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of generated BenchPerson rows being ordered (1,000 or 100,000).")]
[BenchmarkDataShape(typeof(BenchPerson))]
[BenchmarkSla(MaxMeanMs = 100, Standard = "Ordering up to 100k in-memory rows should complete within ~100 ms (comparable to an in-memory LINQ OrderBy).")]
public class SortBenchmarks
{
    private IQueryable<BenchPerson> _queryable = null!;
    private IWhereClauseService _service = null!;
    private SortBy[] _threeKeys = null!;
    private SortBy[] _twoKeys = null!;

    [Params(1_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _service = QueryBenchmarkSupport.CreateWhereClauseService();
        _queryable = QueryBenchmarkSupport.GeneratePeople(RowCount).AsQueryable();
        _twoKeys = [new(nameof(BenchPerson.Age), SortDirection.Asc, 1), new(nameof(BenchPerson.Name), SortDirection.Desc, 2)];
        _threeKeys = [
            new(nameof(BenchPerson.IsActive), SortDirection.Asc, 1), new(nameof(BenchPerson.Age), SortDirection.Desc, 2), new(nameof(BenchPerson.Name), SortDirection.Asc, 3)
        ];
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Single-property ascending sort by Age, then enumerate (baseline).")]
    public int SortByProperty_Single() => _service.SortByProperty(_queryable, nameof(BenchPerson.Age), SortDirection.Asc).Count();

    [Benchmark]
    [BenchmarkDescription("Two-key ordering (Age asc, Name desc) with an Id tie-break, then enumerate.")]
    public int ApplyOrdering_TwoKeys() => _service.ApplyOrdering(_queryable, _twoKeys, p => p.Id, SortDirection.Asc).Count();

    [Benchmark]
    [BenchmarkDescription("Three-key ordering (IsActive, Age desc, Name) with an Id tie-break, then enumerate.")]
    public int ApplyOrdering_ThreeKeys() => _service.ApplyOrdering(_queryable, _threeKeys, p => p.Id, SortDirection.Asc).Count();
}