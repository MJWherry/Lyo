namespace Lyo.Api.Mapping;

/// <summary>Abstraction for mapping objects between API DTOs and EF entities. Implementations can use Mapster, AutoMapper, or custom logic.</summary>
public interface ILyoMapper
{
    /// <summary>Creates a <typeparamref name="TResult" /> from <paramref name="source" /> (new instance).</summary>
    TResult Map<TResult>(object source);

    /// <summary>Copies values from <paramref name="source" /> into an existing <paramref name="destination" />.</summary>
    void Map<TSource, TDest>(TSource source, TDest destination);
}