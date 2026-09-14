using Lyo.Exceptions;

namespace Lyo.IO.FileSystem;

/// <summary>Settings for <see cref="LocalFileSystem" />.</summary>
public sealed class LocalFileSystemOptions
{
    /// <summary>Section name used when binding configuration.</summary>
    public const string SectionName = "LocalFileSystem";

    /// <summary>Root directory this file system is jailed to. Default is <see cref="Path.GetTempPath" />.</summary>
    public string RootPath { get; set; } = Path.GetTempPath();

    /// <summary>Throws when <see cref="RootPath" /> is missing.</summary>
    public void Validate() => ArgumentHelpers.ThrowIfNullOrWhiteSpace(RootPath);
}
