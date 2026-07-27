using Lyo.Api.Mapping;

namespace Lyo.Reporting.Postgres.Mapping;

/// <summary>Tries reporting mapper first; falls back to host mapper for other types.</summary>
public sealed class CompositeLyoMapper(ILyoMapper reporting, ILyoMapper? fallback) : ILyoMapper
{
    public TResult Map<TResult>(object source)
    {
        try {
            return reporting.Map<TResult>(source);
        }
        catch (InvalidOperationException) when (fallback is not null) {
            return fallback.Map<TResult>(source);
        }
    }

    public void Map<TSource, TDest>(TSource source, TDest destination)
    {
        try {
            reporting.Map(source, destination);
        }
        catch (InvalidOperationException) when (fallback is not null) {
            fallback.Map(source, destination);
        }
    }
}
