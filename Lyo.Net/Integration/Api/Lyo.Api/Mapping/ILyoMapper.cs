namespace Lyo.Api.Mapping;

/// <summary>Abstraction for object-to-object mapping. Implementations can use Mapster, AutoMapper, or custom logic.</summary>
public interface ILyoMapper
{
    TResult Map<TResult>(object source);

    void Map<TSource, TDest>(TSource source, TDest destination);
}