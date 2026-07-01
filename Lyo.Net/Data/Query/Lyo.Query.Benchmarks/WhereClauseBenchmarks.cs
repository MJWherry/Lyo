using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using Lyo.Benchmarking;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Services.WhereClause;

namespace Lyo.Query.Benchmarks;

/// <summary>In-memory benchmarks for the where-clause engine: expression build, queryable filtering, and single-entity matching.</summary>
[BenchmarkDescription(
    "Where-clause engine over an in-memory BenchPerson list: a simple single-predicate clause (Age >= 30), a nested boolean clause (IsActive AND (Age > 50 OR Name contains ...)), and a nested-path clause that reaches into the Address object and the Contacts collection (Address.City contains AND Contacts.Count > 1). Covers expression build, IQueryable filtering, and single-entity matching.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of generated BenchPerson rows the clause is applied to (1,000 or 100,000).")]
[BenchmarkDataShape(typeof(BenchPerson), Notes = "Entity with scalar fields, a Tags string collection, a nested Address object, and a Contacts collection of nested objects.")]
public class WhereClauseBenchmarks
{
    private WhereClause _nestedClause = null!;
    private WhereClause _nestedPathClause = null!;
    private IQueryable<BenchPerson> _queryable = null!;
    private IWhereClauseService _service = null!;
    private WhereClause _simpleClause = null!;
    private BenchPerson _single = null!;

    [Params(1_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _service = QueryBenchmarkSupport.CreateWhereClauseService();
        var people = QueryBenchmarkSupport.GeneratePeople(RowCount);
        _queryable = people.AsQueryable();
        _single = people[0];
        _simpleClause = WhereClauseBuilder.And().GreaterThanOrEqual(nameof(BenchPerson.Age), 30).Build();
        _nestedClause = WhereClauseBuilder.And()
            .Equals(nameof(BenchPerson.IsActive), true)
            .AddGroupOr(or => or.GreaterThan(nameof(BenchPerson.Age), 50).Contains(nameof(BenchPerson.Name), "Person 1"))
            .Build();

        // Reaches into the nested Address object and the Contacts collection (dotted member + .Count path).
        _nestedPathClause = WhereClauseBuilder.And().Contains("Address.City", "ville").GreaterThan("Contacts.Count", 1).Build();
    }

    // No Baseline here on purpose: the methods measure unrelated operations (expression compile, full
    // queryable build+scan, single-entity match), so a shared "vs baseline" ratio is meaningless — it
    // produced ~700-10,000x noise comparing a sub-100us compile against a 100k-row scan. Each method is
    // judged on its own SLA instead (mirrors ProjectionBenchmarks).
    [Benchmark]
    [BenchmarkDescription("Compile a LINQ expression tree from the simple single-predicate clause (no data scan).")]
    [BenchmarkSla(MaxMeanUs = 100, Standard = "Compiling a where-clause expression tree is a build-time step and should stay well under a millisecond.")]
    public Expression<Func<BenchPerson, bool>>? BuildExpression_Simple() => ((BaseWhereClauseService)_service).BuildExpressionFromWhereClause<BenchPerson>(_simpleClause);

    [Benchmark]
    [BenchmarkDescription("Compile a LINQ expression tree from the nested AND/OR clause (more nodes to translate).")]
    [BenchmarkSla(MaxMeanUs = 150, Standard = "Compiling a nested where-clause expression tree should stay well under a millisecond.")]
    public Expression<Func<BenchPerson, bool>>? BuildExpression_Nested() => ((BaseWhereClauseService)_service).BuildExpressionFromWhereClause<BenchPerson>(_nestedClause);

    [Benchmark]
    [BenchmarkDescription("Compile a LINQ expression tree from the nested-path clause that traverses Address.City and Contacts.Count.")]
    [BenchmarkSla(MaxMeanUs = 200, Standard = "Compiling a nested-path where-clause (object + collection traversal) should stay well under a millisecond.")]
    public Expression<Func<BenchPerson, bool>>? BuildExpression_NestedPath() => ((BaseWhereClauseService)_service).BuildExpressionFromWhereClause<BenchPerson>(_nestedPathClause);

    [Benchmark]
    [BenchmarkDescription("Apply the simple clause to the full queryable and count matches (build + scan).")]
    [BenchmarkSla(MaxMeanMs = 50, Standard = "Filtering up to 100k in-memory rows should complete within tens of milliseconds.")]
    public int ApplyWhereClause_Simple() => _service.ApplyWhereClause(_queryable, _simpleClause).Count();

    [Benchmark]
    [BenchmarkDescription("Apply the nested clause to the full queryable and count matches (build + scan).")]
    [BenchmarkSla(MaxMeanMs = 75, Standard = "Filtering up to 100k in-memory rows with a nested clause should complete within tens of milliseconds.")]
    public int ApplyWhereClause_Nested() => _service.ApplyWhereClause(_queryable, _nestedClause).Count();

    [Benchmark]
    [BenchmarkDescription("Apply the nested-path clause (Address.City + Contacts.Count) to the full queryable and count matches.")]
    [BenchmarkSla(MaxMeanMs = 100, Standard = "Filtering up to 100k in-memory rows with nested-path traversal should complete within ~100 ms.")]
    public int ApplyWhereClause_NestedPath() => _service.ApplyWhereClause(_queryable, _nestedPathClause).Count();

    [Benchmark]
    [BenchmarkDescription("Evaluate the nested clause against a single entity (no IQueryable, direct match path).")]
    [BenchmarkSla(MaxMeanUs = 20, Standard = "Matching a single entity against a clause should be a few microseconds at most.")]
    public bool MatchesWhereClause_Nested() => _service.MatchesWhereClause(_single, _nestedClause);
}