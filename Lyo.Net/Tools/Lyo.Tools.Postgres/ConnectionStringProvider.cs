namespace Lyo.Tools.Postgres;

/// <summary>
/// Mutable singleton holding the active PostgreSQL connection string for the session. All consumers read ConnectionString at the time of use so changes take effect
/// immediately.
/// </summary>
public sealed class ConnectionStringProvider
{
    public string? ConnectionString { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString);

    /// <summary>Returns the connection string, throwing if none is set.</summary>
    public string GetOrThrow()
        => ConnectionString ?? throw new InvalidOperationException("No connection string is configured. Use 'C. Change Connection String' from the main menu.");

    /// <summary>Returns the connection string truncated at 40 characters for safe display.</summary>
    public string GetMasked()
    {
        if (!IsConfigured)
            return "(not set)";

        var cs = ConnectionString!;
        return cs.Length > 40 ? cs[..40] + "****" : cs;
    }
}