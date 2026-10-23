using Lyo.Exceptions;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.Catalog;

/// <summary>
/// Absolute URI for a catalog static asset. The injected <see cref="HttpClient" /> is often the API factory client, which has no <see cref="HttpClient.BaseAddress" />, so a
/// relative <c>_content/...</c> path is rejected.
/// </summary>
internal static class CatalogAssetUri
{
    /// <summary>Resolves <paramref name="path" /> against the Blazor host. An already-absolute URL is returned unchanged.</summary>
    internal static Uri Resolve(NavigationManager navigation, string path)
    {
        ArgumentHelpers.ThrowIfNull(navigation);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        return navigation.ToAbsoluteUri(path);
    }
}