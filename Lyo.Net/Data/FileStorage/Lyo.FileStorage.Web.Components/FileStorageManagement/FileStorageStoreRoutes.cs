using Lyo.Api.FileStorage.Models;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

/// <summary>Picks the file-storage route prefix from <c>GET stores</c> without changing the metadata query route.</summary>
public static class FileStorageStoreRoutes
{
    /// <summary>Returns the selected store route, the only store's route, or <paramref name="apiPrefix" /> when the list failed or nothing is selected.</summary>
    public static string Prefix(string apiPrefix, IReadOnlyList<FileStorageStoreResponse>? stores, string? selectedName)
    {
        if (stores is { Count: 1 })
            return stores[0].Route;

        if (stores != null && !string.IsNullOrWhiteSpace(selectedName)) {
            foreach (var store in stores) {
                if (string.Equals(store.Name, selectedName, StringComparison.OrdinalIgnoreCase))
                    return store.Route;
            }
        }

        return apiPrefix;
    }
}