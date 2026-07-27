using Lyo.Api.Mapping;
using Lyo.Cache;
using Lyo.Query.Services.ValueConversion;
using Lyo.Query.Services.WhereClause;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Query.Benchmarks;

/// <summary>
/// Self-contained entity used for in-memory where-clause/sort/projection benchmarks (no EF dependencies). Carries a nested <see cref="Address" /> object and a
/// <see cref="Contacts" /> collection so the engine's nested-path handling (e.g. <c>Address.City</c>, <c>Contacts.Count</c>) is exercised, not just flat scalar fields.
/// </summary>
public sealed class BenchPerson
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Age { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<string> Tags { get; set; } = [];

    public BenchAddress Address { get; set; } = new();

    public List<BenchContact> Contacts { get; set; } = [];
}

/// <summary>Nested address on <see cref="BenchPerson" />, used to exercise nested-path filtering/projection.</summary>
public sealed class BenchAddress
{
    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public int PostalCode { get; set; }
}

/// <summary>Nested contact entry inside <see cref="BenchPerson.Contacts" />.</summary>
public sealed class BenchContact
{
    public string Kind { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}

/// <summary>Shared factory helpers for query benchmarks.</summary>
internal static class QueryBenchmarkSupport
{
    private static readonly string[] Cities = ["Perfville", "Benchburg", "Latencyton", "Throughport"];

    internal static BaseWhereClauseService CreateWhereClauseService()
    {
        var cacheOptions = new CacheOptions { Enabled = true };
        var cache = new LocalCacheService(new MemoryCache(new MemoryCacheOptions()), new NullLogger<LocalCacheService>(), cacheOptions);
        IValueConversionService valueConversion = new ValueConversionService();
        return new(cache, cacheOptions, valueConversion);
    }

    internal static List<BenchPerson> GeneratePeople(int count)
    {
        var people = new List<BenchPerson>(count);
        var baseDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < count; i++) {
            people.Add(
                new() {
                    Id = Guid.NewGuid(),
                    Name = $"Person {i}",
                    Age = 18 + i % 60,
                    IsActive = i % 2 == 0,
                    CreatedAt = baseDate.AddMinutes(i),
                    Tags = [$"tag-{i % 5}", $"group-{i % 3}"],
                    Address = new() { City = Cities[i % Cities.Length], Country = "Lyoland", PostalCode = 10000 + i % 9000 },
                    Contacts = [new() { Kind = "email", Value = $"person{i}@example.com" }, new() { Kind = "phone", Value = $"+1-555-{i % 10000:D4}" }]
                });
        }

        return people;
    }
}

/// <summary>ILyoMapper implementation that delegates to Mapster's IMapper.</summary>
internal sealed class MapsterLyoMapper(IMapper mapster) : ILyoMapper
{
    public TResult Map<TResult>(object source) => mapster.Map<TResult>(source);

    public void Map<TSource, TDest>(TSource source, TDest destination) => mapster.Map(source, destination);
}