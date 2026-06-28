using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lyo.Api.Mapping;
using Lyo.Benchmarking;
using Mapster;
using MapsterMapper;

namespace Lyo.Query.Benchmarks;

/// <summary>Benchmarks object-to-DTO mapping through <see cref="ILyoMapper" /> (Mapster) for single objects and collections with nested children.</summary>
[BenchmarkDescription("Object-to-DTO mapping via ILyoMapper (Mapster): a single entity and a 100-entity list, each carrying a nested child collection. ChildCount scales the nested collection so mapping cost reflects graph depth, not just row count.")]
[BenchmarkParameter("ChildCount", Unit = "children", Description = "Number of nested MapChild items per entity (0, 5, 25); the dominant cost driver beyond the flat fields.")]
[BenchmarkDataShape(typeof(MapEntity), Notes = "Entity with scalar fields plus a Children collection of MapChild (nested object), illustrating the nested mapping the row count alone hides.")]
[BenchmarkSla(MaxMeanMs = 5, Standard = "Object-to-DTO mapping of a single entity or a 100-entity list with nested children should be a low single-digit milliseconds at most.")]
public class MappingBenchmarks
{
    private ILyoMapper _mapper = null!;
    private MapEntity _entity = null!;
    private List<MapEntity> _entities = null!;

    [Params(0, 5, 25)]
    public int ChildCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var config = new TypeAdapterConfig();
        config.NewConfig<MapEntity, MapEntityRes>();
        config.NewConfig<MapChild, MapChildRes>();
        config.Compile();
        IMapper mapster = new Mapper(config);
        _mapper = new MapsterLyoMapper(mapster);

        _entity = CreateEntity(0);
        _entities = Enumerable.Range(0, 100).Select(CreateEntity).ToList();
    }

    private MapEntity CreateEntity(int seed) => new() {
        Id = Guid.NewGuid(),
        Name = $"Entity {seed}",
        Age = 20 + seed % 50,
        Children = Enumerable.Range(0, ChildCount)
            .Select(c => new MapChild { Id = Guid.NewGuid(), Label = $"child-{c}", Value = c })
            .ToList()
    };

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Map one entity (with its ChildCount nested children) to its response DTO (baseline).")]
    public MapEntityRes Map_Single() => _mapper.Map<MapEntityRes>(_entity);

    [Benchmark]
    [BenchmarkDescription("Map a 100-entity list (each with ChildCount nested children) to response DTOs.")]
    public List<MapEntityRes> Map_List() => _mapper.Map<List<MapEntityRes>>(_entities);
}

public sealed class MapEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public List<MapChild> Children { get; set; } = [];
}

public sealed class MapChild
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
}

public sealed record MapEntityRes(Guid Id, string Name, int Age, List<MapChildRes> Children);

public sealed record MapChildRes(Guid Id, string Label, int Value);
