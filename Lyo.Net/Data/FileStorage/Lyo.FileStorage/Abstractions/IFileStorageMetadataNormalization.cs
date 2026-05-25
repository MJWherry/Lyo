namespace Lyo.FileStorage.Abstractions;

/// <summary>Tenant/path/content-type normalization used when persisting metadata for uploads and copies.</summary>
internal interface IFileStorageMetadataNormalization
{
    string? ResolveTenantId(string? explicitTenantId);

    string ResolveStoredContentType(string? declaredContentType, string? originalFileName);

    string? NormalizePathPrefix(string? pathPrefix);
}
