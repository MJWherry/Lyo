using Lyo.Exceptions;

namespace Lyo.IO.FileSystem;

/// <summary>Settings for <see cref="LocalFileSystem" />.</summary>
public sealed class LocalFileSystemOptions
{
    /// <summary>Section name used when binding configuration.</summary>
    public const string SectionName = "LocalFileSystem";

    /// <summary>Root directory this file system is jailed to. Default is <see cref="Path.GetTempPath" />.</summary>
    public string RootPath { get; set; } = Path.GetTempPath();

    /// <summary>
    /// Creates <see cref="RootPath" /> at construction when it does not exist. Default is true. Set false when the root is a mount that must already be present and an
    /// empty directory appearing in its place would mask a misconfiguration.
    /// </summary>
    public bool CreateRootIfMissing { get; set; } = true;

    /// <summary>Throws when <see cref="RootPath" /> is missing.</summary>
    public void Validate() => ArgumentHelpers.ThrowIfNullOrWhiteSpace(RootPath);
}
