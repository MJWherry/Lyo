using Lyo.Api.Models.Common.Response;
using Lyo.Exceptions;

namespace Lyo.Api.Client;

/// <summary>
/// Metadata helpers for Lyo.Api <c>GET {baseRoute}/Metadata</c> (typed CreateBuilder and dynamic CRUD).
/// </summary>
public static class ApiClientMetadataExtensions
{
    /// <summary>Typed CreateBuilder metadata: <c>GET {baseRoute}/Metadata</c> → <see cref="EndpointMetadataResponse" />.</summary>
    public static Task<EndpointMetadataResponse?> GetMetadataAsync(
        this IApiClient client,
        string baseRoute,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(client);
        return client.GetAsAsync<EndpointMetadataResponse>(MetadataPath(baseRoute), before, ct);
    }

    /// <summary>Dynamic CRUD registry metadata: <c>GET {baseRoute}/Metadata</c> → <see cref="CrudMetadataResponse" />.</summary>
    public static Task<CrudMetadataResponse?> GetCrudMetadataAsync(
        this IApiClient client,
        string baseRoute,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(client);
        return client.GetAsAsync<CrudMetadataResponse>(MetadataPath(baseRoute), before, ct);
    }

    /// <summary>Dynamic CRUD per-entity metadata: <c>GET {baseRoute}/{entityType}/Metadata</c>.</summary>
    public static Task<EntityTypeMetadata?> GetEntityMetadataAsync(
        this IApiClient client,
        string baseRoute,
        string entityType,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(client);
        ArgumentHelpers.ThrowIfNull(entityType);
        return client.GetAsAsync<EntityTypeMetadata>(EntityMetadataPath(baseRoute, entityType), before, ct);
    }

    /// <summary>Builds <c>{baseRoute}/Metadata</c> (leading slash, no trailing slash on the prefix).</summary>
    public static string MetadataPath(string baseRoute) => $"{NormalizeRoutePrefix(baseRoute)}/Metadata";

    /// <summary>Builds <c>{baseRoute}/{entityType}/Metadata</c>.</summary>
    public static string EntityMetadataPath(string baseRoute, string entityType)
    {
        ArgumentHelpers.ThrowIfNull(entityType);
        var encoded = Uri.EscapeDataString(entityType.Trim());
        return $"{NormalizeRoutePrefix(baseRoute)}/{encoded}/Metadata";
    }

    static string NormalizeRoutePrefix(string? baseRoute)
    {
        var trimmed = (baseRoute ?? string.Empty).Trim().Trim('/');
        return trimmed.Length == 0 ? string.Empty : "/" + trimmed;
    }
}
