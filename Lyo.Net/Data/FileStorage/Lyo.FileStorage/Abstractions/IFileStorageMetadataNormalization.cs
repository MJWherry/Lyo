namespace Lyo.FileStorage.Abstractions;

/// <summary>Normalizes tenant, path, and content-type values before metadata is written for uploads and copies.</summary>
internal interface IFileStorageMetadataNormalization
{
    string? ResolveTenantId(string? explicitTenantId);

    string ResolveStoredContentType(string? declaredContentType, string? originalFileName);

    string? NormalizePathPrefix(string? pathPrefix);
}