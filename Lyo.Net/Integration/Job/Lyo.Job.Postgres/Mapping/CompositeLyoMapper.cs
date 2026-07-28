using Lyo.Api.Mapping;

namespace Lyo.Job.Postgres.Mapping;

/// <summary>Tries <paramref name="job" /> first for job types; falls back to <paramref name="fallback" /> for host/domain types.</summary>
public sealed class CompositeLyoMapper(ILyoMapper job, ILyoMapper? fallback) : ILyoMapper
{
    public TResult Map<TResult>(object source)
    {
        try {
            return job.Map<TResult>(source);
        }
        catch (InvalidOperationException) when (fallback is not null) {
            return fallback.Map<TResult>(source);
        }
    }

    public void Map<TSource, TDest>(TSource source, TDest destination)
    {
        try {
            job.Map(source, destination);
        }
        catch (InvalidOperationException) when (fallback is not null) {
            fallback.Map(source, destination);
        }
    }
}