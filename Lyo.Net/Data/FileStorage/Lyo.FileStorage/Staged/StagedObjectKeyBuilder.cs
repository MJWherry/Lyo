using Lyo.Common.Extensions;

namespace Lyo.FileStorage.Staged;

/// <summary>Shared object-key layout for staged uploads across Local, S3, and Blob backends.</summary>
public static class StagedObjectKeyBuilder
{
    /// <summary>Builds <c>{storagePrefix}/{pathPrefix}/.stage/{stageId:N}/object</c>, omitting empty prefix segments. Used by all <see cref="IStagedFilePhysicalIO" /> implementations.</summary>
    public static string Build(Guid stageId, string? pathPrefix, string? storagePrefix)
    {
        var parts = new List<string>();
        if (!storagePrefix.IsNullOrWhitespace())
            parts.Add(storagePrefix.Trim().TrimStart('/', '\\').TrimEnd('/', '\\'));

        if (!pathPrefix.IsNullOrWhitespace())
            parts.Add(pathPrefix.Trim().Trim('/'));

        parts.Add(".stage");
        parts.Add(stageId.ToString("N"));
        parts.Add("object");
        return string.Join("/", parts);
    }
}