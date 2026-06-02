using System.Diagnostics;

namespace Lyo.Comic.Api.Models.Response;

/// <summary>Request body for retrieving multiple files by their IDs.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FilesBatchReq(IReadOnlyList<Guid> Ids)
{
    public override string ToString()
        => $"FilesBatchReq: {Ids.Count} id(s)";
}

/// <summary>A single file entry returned in a batch file retrieval response.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileBatchEntry(Guid Id, string ContentType, string Data)
{
    public override string ToString()
        => $"FileBatchEntry: {Id}, {ContentType}, {Data.Length} chars";
}
