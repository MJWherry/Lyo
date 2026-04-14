using Lyo.Cache;
using Lyo.Query.Services.ValueConversion;
using Lyo.Query.Services.WhereClause;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Query.Tests;

public class WhereClauseServiceTests
{
    protected BaseWhereClauseService CreateService()
    {
        var logger = new NullLogger<LocalCacheService>();
        var cacheOptions = new CacheOptions { Enabled = false };
        var cache = new LocalCacheService(new MemoryCache(new MemoryCacheOptions()), logger, cacheOptions);
        IValueConversionService valueConversion = new TestValueConversionService();
        return new(cache, cacheOptions, valueConversion);
    }
}