namespace Lyo.Api.Services.Crud;

public interface ISprocService<in TContext>
{
    Task<IReadOnlyList<TResult>> ExecuteStoredProcAsync<TResult>(
        string storedProcName,
        IReadOnlyDictionary<string, object?>? parameters = null,
        string[]? extraCacheTags = null,
        CancellationToken ct = default);
}