using Lyo.Api.Mapping;
using MapsterMapper;

namespace Lyo.Tools;

/// <summary>ILyoMapper that forwards work to Mapster's IMapper. Used by TestApi and TestConsole.</summary>
internal sealed class MapsterLyoMapper(IMapper mapster) : ILyoMapper
{
    public TResult Map<TResult>(object source) => mapster.Map<TResult>(source);

    public void Map<TSource, TDest>(TSource source, TDest destination) => mapster.Map(source, destination);
}
