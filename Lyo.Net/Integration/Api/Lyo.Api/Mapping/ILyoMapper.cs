namespace Lyo.Api.Mapping;

/// <summary>Abstraction for object-to-object mapping between API DTOs and EF entities. Implementations can use Mapster, AutoMapper, or custom logic.</summary>
public interface ILyoMapper
{
    /// <summary>Creates <typeparamref name="TResult" /> from <paramref name="source" /> (new instance).</summary>
    TResult Map<TResult>(object source);

    /// <summary>Copies values from <paramref name="source" /> into existing <paramref name="destination" />.</summary>
    void Map<TSource, TDest>(TSource source, TDest destination);
}