using System.Text;
using Lyo.Cache.Fusion;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Cache.Benchmarks;

/// <summary>Shared helpers and payload types for cache benchmarks.</summary>
internal static class CacheBenchmarkSupport
{
    internal static ICacheService CreateLocal(Action<CacheOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLocalCache(o => {
            o.Enabled = true;
            configure?.Invoke(o);
        });

        return services.BuildServiceProvider().GetRequiredService<ICacheService>();
    }

    internal static ICacheService CreateFusion(Action<CacheOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddFusionCache(o => {
            o.Enabled = true;
            configure?.Invoke(o);
        });

        return services.BuildServiceProvider().GetRequiredService<ICacheService>();
    }

    /// <summary>Builds a repeating, compressible string of approximately <paramref name="sizeBytes" /> ASCII bytes.</summary>
    internal static string CompressibleString(int sizeBytes)
    {
        const string seed = "the quick brown fox jumps over the lazy dog 0123456789 ";
        var builder = new StringBuilder(sizeBytes + seed.Length);
        while (builder.Length < sizeBytes)
            builder.Append(seed);

        return builder.ToString(0, sizeBytes);
    }

    /// <summary>Builds a nested payload (object + collection + dictionary) whose <see cref="NestedCachePayload.Data" /> body carries about <paramref name="approxBytes" /> bytes.</summary>
    internal static NestedCachePayload GenerateNested(int approxBytes)
        => new() {
            Id = 1,
            Name = "nested-payload",
            Address = new() {
                Street = "123 Benchmark Way",
                City = "Perfville",
                Country = "Lyoland",
                Geo = new() { Latitude = 51.5074, Longitude = -0.1278 }
            },
            Contacts = Enumerable.Range(0, 5).Select(i => new CacheContact { Kind = i % 2 == 0 ? "email" : "phone", Value = $"contact-{i}@example.com" }).ToList(),
            Attributes = new() { ["tier"] = "gold", ["region"] = "us-east", ["source"] = "benchmark" },
            Data = CompressibleString(approxBytes)
        };
}

/// <summary>Small flat cached value used to exercise the object and payload cache paths.</summary>
public sealed class CachePayload
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Data { get; set; } = string.Empty;
}

/// <summary>
/// Cached value with realistic nested structure: a nested <see cref="Address" /> object (itself nesting a <see cref="CacheGeo" />), a <see cref="Contacts" /> collection of
/// objects, an <see cref="Attributes" /> dictionary, and a size-scalable <see cref="Data" /> body. Exercises serialization/compression of non-trivial graphs.
/// </summary>
public sealed class NestedCachePayload
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public CacheAddress Address { get; set; } = new();

    public List<CacheContact> Contacts { get; set; } = [];

    public Dictionary<string, string> Attributes { get; set; } = [];

    public string Data { get; set; } = string.Empty;
}

/// <summary>Nested address with a further-nested geo coordinate (depth-2 object graph).</summary>
public sealed class CacheAddress
{
    public string Street { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public CacheGeo Geo { get; set; } = new();
}

/// <summary>Geo coordinate nested inside <see cref="CacheAddress" />.</summary>
public sealed class CacheGeo
{
    public double Latitude { get; set; }

    public double Longitude { get; set; }
}

/// <summary>Contact entry nested inside the payload's contacts collection.</summary>
public sealed class CacheContact
{
    public string Kind { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}