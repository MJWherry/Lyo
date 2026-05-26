namespace Lyo.FileStorage.Models;

/// <summary>Parameters for copying an existing stored file under a new file id.</summary>
public sealed record CopyFileRequest
{
    /// <summary>If set, target object uses this path prefix; otherwise inherits the source prefix.</summary>
    public string? PathPrefix { get; init; }
}