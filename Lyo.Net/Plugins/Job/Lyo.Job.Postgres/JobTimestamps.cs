namespace Lyo.Job.Postgres;

/// <summary>
/// UTC normalization for job timestamps. Npgsql maps <c>timestamp with time zone</c> to <see cref="DateTimeKind.Utc" /> and throws on a <see cref="DateTimeKind.Local" /> value, so
/// every timestamp arriving from a request body or a client clock has to pass through here before it reaches the context.
/// </summary>
public static class JobTimestamps
{
    /// <summary>Returns the value as UTC. Local values are converted. Unspecified values are assumed to already be UTC and are only re-tagged.</summary>
    /// <param name="value">Timestamp being normalized.</param>
    public static DateTime ToUtc(DateTime value)
        => value.Kind switch {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            var _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    /// <summary>Nullable overload of <see cref="ToUtc(DateTime)" />.</summary>
    /// <param name="value">Timestamp being normalized, or null.</param>
    public static DateTime? ToUtc(DateTime? value) => value.HasValue ? ToUtc(value.Value) : null;
}
