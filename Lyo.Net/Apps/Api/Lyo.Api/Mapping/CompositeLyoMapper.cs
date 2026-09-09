namespace Lyo.Api.Mapping;

/// <summary>Chains two mappers. <paramref name="primary" /> handles the types it knows; anything else falls through to <paramref name="fallback" />.</summary>
/// <remarks>
/// A feature package registers its own <see cref="ILyoMapper" /> (for example <c>JobLyoMapper</c>) covering only its DTOs. A host that also maps its own domain types wraps the
/// two so both work through one <see cref="ILyoMapper" /> registration. Fall-through is driven by <see cref="InvalidOperationException" />, which is what a Lyo mapper
/// throws for a pair it has no mapping for.
/// </remarks>
/// <param name="primary">Tried first. Usually the feature package mapper.</param>
/// <param name="fallback">Used when <paramref name="primary" /> has no mapping. When null, the primary exception propagates.</param>
public sealed class CompositeLyoMapper(ILyoMapper primary, ILyoMapper? fallback) : ILyoMapper
{
    /// <inheritdoc />
    public TResult Map<TResult>(object source)
    {
        try {
            return primary.Map<TResult>(source);
        }
        catch (InvalidOperationException) when (fallback is not null) {
            return fallback.Map<TResult>(source);
        }
    }

    /// <inheritdoc />
    public void Map<TSource, TDest>(TSource source, TDest destination)
    {
        try {
            primary.Map(source, destination);
        }
        catch (InvalidOperationException) when (fallback is not null) {
            fallback.Map(source, destination);
        }
    }
}
