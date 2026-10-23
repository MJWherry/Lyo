using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.FileMetadataStore;

/// <summary>Options for the local-disk file metadata store.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class LocalFileMetadataStoreOptions
{
    public const string SectionName = "LocalFileMetadataStore";

    /// <summary>Root directory for metadata files.</summary>
    public string RootDirectoryPath { get; set; } = string.Empty;

    /// <summary>When true, the root directory is created if it is missing. Starts as true.</summary>
    public bool CreateDirectoryIfNotExists { get; set; } = true;

    /// <summary>Throws when <see cref="RootDirectoryPath" /> is missing.</summary>
    public void Validate() => ArgumentHelpers.ThrowIfNullOrWhiteSpace(RootDirectoryPath);

    /// <summary>Metadata root directory and whether a missing root is created.</summary>
    public override string ToString() => $"RootDirectoryPath={RootDirectoryPath} CreateDirectoryIfNotExists={CreateDirectoryIfNotExists}";
}