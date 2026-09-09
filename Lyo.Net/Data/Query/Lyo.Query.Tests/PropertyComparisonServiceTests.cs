using Lyo.Cache;
using Lyo.Query.Services.PropertyComparison;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Query.Tests;

public class PropertyComparisonServiceTests
{
    private static PropertyComparisonService CreateService()
    {
        var logger = new NullLogger<LocalCacheService>();
        var cacheOptions = new CacheOptions { Enabled = false };
        var cache = new LocalCacheService(new MemoryCache(new MemoryCacheOptions()), logger, cacheOptions);
        return new(cache, cacheOptions);
    }

    [Fact]
    public void GetPropertyDifferences_WhenValuesDiffer_ReturnsDifferences()
    {
        var svc = CreateService();
        var entity = new PersonBuilder().WithName("Alice").WithAge(30).Build();
        var newData = new { Name = "Bob", Age = 30 };
        var diff = svc.GetPropertyDifferences(entity, newData);
        Assert.Single(diff);
        Assert.Equal("Bob", diff["Name"]);
    }

    [Fact]
    public void GetPropertyDifferences_WhenAllMatch_ReturnsEmpty()
    {
        var svc = CreateService();
        var entity = new PersonBuilder().WithName("Alice").WithAge(30).Build();
        var newData = new { Name = "Alice", Age = 30 };
        var diff = svc.GetPropertyDifferences(entity, newData);
        Assert.Empty(diff);
    }

    [Fact]
    public void GetPropertyDifferences_WhenMultipleDiffer_ReturnsAll()
    {
        var svc = CreateService();
        var entity = new PersonBuilder().WithName("Alice").WithAge(30).Build();
        var newData = new { Name = "Bob", Age = 25 };
        var diff = svc.GetPropertyDifferences(entity, newData);
        Assert.Equal(2, diff.Count);
        Assert.Equal("Bob", diff["Name"]);
        Assert.Equal(25, diff["Age"]);
    }

    [Fact]
    public void GetPropertyDifferences_IgnoresPropertiesNotOnRequest()
    {
        var svc = CreateService();
        var entity = new PersonBuilder().WithName("Alice").WithAge(30).WithId(Guid.NewGuid()).Build();
        var newData = new { Name = "Alice" };
        var diff = svc.GetPropertyDifferences(entity, newData);
        Assert.Empty(diff);
    }
}