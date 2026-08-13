using System.Diagnostics;

namespace Lyo.Comic.Api.Models.Request;

/// <summary>Request body for <c>POST /files/archive</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FilesArchiveReq(IReadOnlyList<FilesArchiveEntryReq> Entries, string? FileName = null)
{
    public override string ToString() => $"FilesArchiveReq: {Entries.Count} entries, FileName={FileName ?? "(default)"}";
}

/// <summary>One file to include in an archive zip.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FilesArchiveEntryReq(Guid Id, string? Path = null)
{
    public override string ToString() => Path is null ? $"FilesArchiveEntryReq: {Id}" : $"FilesArchiveEntryReq: {Id} -> {Path}";
}
