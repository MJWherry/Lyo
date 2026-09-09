namespace Lyo.Tools.Postgres;

/// <summary>
/// Session-scoped mutable singleton for the active PostgreSQL connection string. Callers read ConnectionString when they use it so a change is visible
/// right away.
/// </summary>
public sealed class ConnectionStringProvider
{
    public string? ConnectionString { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString);

    /// <summary>The current connection string, or an exception if it has not been set.</summary>
    public string GetOrThrow()
        => ConnectionString ?? throw new InvalidOperationException("No connection string is configured. Use 'C. Change Connection String' from the main menu.");

    /// <summary>Connection string clipped to 40 characters for safe display.</summary>
    public string GetMasked()
    {
        if (!IsConfigured)
            return "(not set)";

        var cs = ConnectionString!;
        return cs.Length > 40 ? cs[..40] + "****" : cs;
    }
}