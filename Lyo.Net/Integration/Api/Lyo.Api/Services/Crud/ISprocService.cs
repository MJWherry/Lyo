namespace Lyo.Api.Services.Crud;

/// <summary>Executes database set-returning routines and maps rows to <c>TResult</c> (PostgreSQL implementation ships with this package).</summary>
public interface ISprocService
{
    /// <summary>Runs <c>SELECT * FROM …</c> for the named function, optionally with parameters and extra cache tags.</summary>
    Task<IReadOnlyList<TResult>> ExecuteStoredProcAsync<TResult>(
        string storedProcName,
        IReadOnlyDictionary<string, object?>? parameters = null,
        string[]? extraCacheTags = null,
        CancellationToken ct = default);
}