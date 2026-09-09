using System.Diagnostics;
using System.Linq.Expressions;
using System.Text.Json;
using Lyo.Cache;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Enums;
using Lyo.Query.Models.Exceptions;
using Lyo.Query.Services.ValueConversion;
using Lyo.Query.Services.WhereClause;
using Lyo.Query.Tests.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Query.Tests;

/// <summary>
/// Regression suite for query-stack hardening: cache-key collisions that returned another request's rows, predicate entries evicted by unrelated data writes, and
/// unbounded regex evaluation.
/// </summary>
public class QueryStackHardeningTests
{
    private static readonly Guid G1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid G2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void GetWhereClauseTreeHash_InClauseWithDifferentValues_ProducesDifferentHashes()
    {
        var a = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.In, new[] { G1 });
        var b = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.In, new[] { G2 });
        Assert.NotEqual(WhereClauseHelpers.GetWhereClauseTreeHash(a), WhereClauseHelpers.GetWhereClauseTreeHash(b));
    }

    [Fact]
    public void GetWhereClauseTreeHash_InClauseWithDifferentArity_ProducesDifferentHashes()
    {
        var one = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.In, new[] { G1 });
        var two = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.In, new[] { G1, G2 });
        Assert.NotEqual(WhereClauseHelpers.GetWhereClauseTreeHash(one), WhereClauseHelpers.GetWhereClauseTreeHash(two));
    }

    [Fact]
    public void GetWhereClauseTreeHash_ListAndArrayWithSameContents_ProduceSameHash()
    {
        var array = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.In, new[] { G1, G2 });
        var list = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.In, new List<Guid> { G1, G2 });
        Assert.Equal(WhereClauseHelpers.GetWhereClauseTreeHash(array), WhereClauseHelpers.GetWhereClauseTreeHash(list));
    }

    /// <summary>Element values must not forge a different tree by embedding the separator the hash uses between list items.</summary>
    [Fact]
    public void GetWhereClauseTreeHash_ValuesContainingSeparators_DoNotCollide()
    {
        var a = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.In, new[] { "a,b", "c" });
        var b = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.In, new[] { "a", "b,c" });
        Assert.NotEqual(WhereClauseHelpers.GetWhereClauseTreeHash(a), WhereClauseHelpers.GetWhereClauseTreeHash(b));
    }

    [Fact]
    public void GetWhereClauseTreeHash_JsonElementValues_MatchEquivalentClrValues()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("\"abc\"");
        var fromJson = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Equals, json);
        var fromClr = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Equals, "abc");
        Assert.Equal(WhereClauseHelpers.GetWhereClauseTreeHash(fromClr), WhereClauseHelpers.GetWhereClauseTreeHash(fromJson));
    }

    /// <summary>
    /// A compiled predicate is a pure function of clause shape and entity type, so a row write must not drop it. Tagging predicate entries with the same
    /// <c>entity:{name}</c> tag the result cache uses forced a recompile on every write.
    /// </summary>
    [Fact]
    public async Task ApplyWhereClause_PredicateCache_SurvivesEntityDataInvalidation()
    {
        var (service, cache) = CreateCountingService();
        // The compiled-predicate cache is process-wide, so the clause must be unique here or another test's identical clause pre-warms it and the count starts at zero.
        var name = $"Alice-{Guid.NewGuid():N}";
        var people = new List<Person> { new PersonBuilder().WithName(name).Build() }.AsQueryable();
        var node = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Equals, name);

        service.ApplyWhereClause(people, node).ToList();
        var afterFirst = service.BuildCount;
        Assert.Equal(1, afterFirst);

        service.ApplyWhereClause(people, node).ToList();
        Assert.Equal(afterFirst, service.BuildCount);

        await cache.InvalidateQueryCacheAsync<Person>();
        await cache.InvalidateCacheItemByTag("entity:person");

        service.ApplyWhereClause(people, node).ToList();
        Assert.Equal(afterFirst, service.BuildCount);
    }

    [Fact]
    public void ApplyWhereClause_RegexPatternExceedingMaxLength_ThrowsInvalidQuery()
    {
        var svc = CreateService();
        var people = new List<Person> { new PersonBuilder().WithName("Alice").Build() }.AsQueryable();
        var pattern = new string('a', 5_000);
        var node = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Regex, pattern);
        Assert.Throws<InvalidQueryException>(() => svc.ApplyWhereClause(people, node).ToList());
    }

    [Fact]
    public void ApplyWhereClause_UncompilableRegexPattern_ThrowsInvalidQuery()
    {
        var svc = CreateService();
        var people = new List<Person> { new PersonBuilder().WithName("Alice").Build() }.AsQueryable();
        var node = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Regex, "([unclosed");
        Assert.Throws<InvalidQueryException>(() => svc.ApplyWhereClause(people, node).ToList());
    }

    /// <summary>Catastrophic backtracking must abort on the match timeout instead of pinning a core forever.</summary>
    [Fact]
    public void ApplyWhereClause_CatastrophicRegex_AbortsWithinTimeout()
    {
        var svc = CreateService();
        var people = new List<Person> { new PersonBuilder().WithName(new string('a', 40) + "!").Build() }.AsQueryable();
        var node = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Regex, "^(a+)+$");

        var stopwatch = Stopwatch.StartNew();
        Assert.ThrowsAny<Exception>(() => svc.ApplyWhereClause(people, node).ToList());
        stopwatch.Stop();
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(20), $"Regex evaluation took {stopwatch.Elapsed}, expected an early abort.");
    }

    private static BaseWhereClauseService CreateService()
    {
        var cacheOptions = new CacheOptions { Enabled = false };
        var cache = new LocalCacheService(new MemoryCache(new MemoryCacheOptions()), new NullLogger<LocalCacheService>(), cacheOptions);
        return new(cache, cacheOptions, new TestValueConversionService());
    }

    private static (CountingWhereClauseService Service, ICacheService Cache) CreateCountingService()
    {
        var cacheOptions = new CacheOptions { Enabled = true };
        var cache = new LocalCacheService(new MemoryCache(new MemoryCacheOptions()), new NullLogger<LocalCacheService>(), cacheOptions);
        return (new(cache, cacheOptions, new TestValueConversionService()), cache);
    }

    private sealed class CountingWhereClauseService(ICacheService cache, CacheOptions cacheOptions, IValueConversionService valueConversion)
        : BaseWhereClauseService(cache, cacheOptions, valueConversion)
    {
        private int _buildCount;

        public int BuildCount => Volatile.Read(ref _buildCount);

        public override Expression<Func<TEntity, bool>>? BuildExpressionFromWhereClause<TEntity>(Lyo.Query.Models.Common.WhereClause? queryNode, bool includeSubClauses = true)
        {
            Interlocked.Increment(ref _buildCount);
            return base.BuildExpressionFromWhereClause<TEntity>(queryNode, includeSubClauses);
        }
    }
}
