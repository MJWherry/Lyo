using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using Lyo.Api.Mapping;
using Lyo.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lyo.Api.Services.Crud;

/// <summary>PostgreSQL: runs set-returning functions via <c>SELECT * FROM schema.func(@a, @b, …)</c>, maps rows with <see cref="ILyoMapper" />, and caches like the legacy MSSQL path.</summary>
public sealed class PostgresSprocService<TContext>(
    IDbContextFactory<TContext> contextFactory,
    ILyoMapper mapper,
    ICacheService cache,
    ILogger<PostgresSprocService<TContext>> logger) : ISprocService<TContext>
    where TContext : DbContext
{
    private static readonly Regex SprocNameRegex = new(@"^[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ParameterNameRegex = new(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<IReadOnlyList<TResult>> ExecuteStoredProcAsync<TResult>(
        string storedProcName,
        IReadOnlyDictionary<string, object?>? parameters = null,
        string[]? extraCacheTags = null,
        CancellationToken ct = default)
    {
        logger.LogDebug("Executing {StoredProcedureName} with {StoredProcedureParameterCount} parameters", storedProcName, parameters?.Count);
        if (!SprocNameRegex.IsMatch(storedProcName))
            throw new ArgumentException("Stored procedure name contains invalid characters.", nameof(storedProcName));

        extraCacheTags ??= [];
        var cacheKey = GenerateCacheKey<TResult>(storedProcName, parameters);
        var cacheTags = new List<string>(2 + extraCacheTags.Length) { "sprocs", $"sprocs:{typeof(TResult).Name}" };
        cacheTags.AddRange(extraCacheTags);
        var list = await cache.GetOrSetAsync(cacheKey, async token => await ExecuteCoreAsync<TResult>(storedProcName, parameters, token).ConfigureAwait(false), cacheTags, ct)
            .ConfigureAwait(false);

        return list ?? [];
    }

    private async Task<IReadOnlyList<TResult>> ExecuteCoreAsync<TResult>(string storedProcName, IReadOnlyDictionary<string, object?>? parameters, CancellationToken ct)
    {
        try {
            await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var connection = context.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            if (parameters is not null && parameters.Count > 0) {
                foreach (var kvp in parameters) {
                    var name = kvp.Key.TrimStart('@');
                    if (!ParameterNameRegex.IsMatch(name))
                        throw new ArgumentException($"Parameter name '{kvp.Key}' is not a valid PostgreSQL identifier.", nameof(parameters));
                }

                var refs = string.Join(", ", parameters.Keys.Select(k => "@" + k.TrimStart('@')));
                command.CommandText = $"SELECT * FROM {storedProcName}({refs})";
                foreach (var kvp in parameters) {
                    var name = kvp.Key.TrimStart('@');
                    var p = command.CreateParameter();
                    p.ParameterName = name;
                    p.Value = kvp.Value ?? DBNull.Value;
                    command.Parameters.Add(p);
                }
            }
            else
                command.CommandText = $"SELECT * FROM {storedProcName}()";

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(ct).ConfigureAwait(false);

            await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
            var results = new List<TResult>();
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
                results.Add(MapReaderToType<TResult>(reader));

            return results;
        }
        catch (Exception ex) when (ex is not ArgumentException) {
            throw new InvalidOperationException($"Couldn't execute sproc {storedProcName}", ex);
        }
    }

    private T MapReaderToType<T>(IDataReader reader)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++) {
            var columnName = reader.GetName(i);
            var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
            dict[columnName] = value;
        }

        return mapper.Map<T>(dict);
    }

    private static string GenerateCacheKey<T>(string storedProcName, IReadOnlyDictionary<string, object?>? parameters)
    {
        var sb = new StringBuilder();
        sb.Append($"sproc:{storedProcName.ToLowerInvariant()}|entity:{typeof(T).Name.ToLowerInvariant()}|");
        if (parameters is null)
            return sb.ToString();

        foreach (var kvp in parameters.OrderBy(k => k.Key)) {
            if (kvp.Value is not null)
                sb.Append($"{kvp.Key}={kvp.Value};");
        }

        return sb.ToString();
    }
}