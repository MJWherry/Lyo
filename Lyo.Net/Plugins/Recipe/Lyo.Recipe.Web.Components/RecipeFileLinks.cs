namespace Lyo.Recipe.Web.Components;

/// <summary>Download URLs for FileStorage files linked from a recipe subject.</summary>
public static class RecipeFileLinks
{
    /// <summary>Relative or absolute download URL. <paramref name="inline" /> asks the browser to display the file.</summary>
    public static string DownloadUrl(string? fileStoragePrefix, string? apiOrigin, Guid fileId, bool inline)
    {
        var prefix = (fileStoragePrefix ?? "").Trim().Trim('/');
        var origin = string.IsNullOrWhiteSpace(apiOrigin) ? "" : apiOrigin.TrimEnd('/');
        var path = $"{prefix}/files/{fileId:D}/download";
        if (inline)
            path += "?inline=true";

        return string.IsNullOrWhiteSpace(origin) ? path : $"{origin}/{path}";
    }
}