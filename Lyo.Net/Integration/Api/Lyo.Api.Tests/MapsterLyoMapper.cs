using Lyo.Api.Mapping;
using MapsterMapper;

namespace Lyo.Api.Tests;

/// <summary>ILyoMapper implementation that delegates to Mapster's IMapper.</summary>
internal sealed class MapsterLyoMapper(IMapper mapster) : ILyoMapper
{
    public TResult Map<TResult>(object source) => mapster.Map<TResult>(source);

    public void Map<TSource, TDest>(TSource source, TDest destination) => mapster.Map(source, destination);
}