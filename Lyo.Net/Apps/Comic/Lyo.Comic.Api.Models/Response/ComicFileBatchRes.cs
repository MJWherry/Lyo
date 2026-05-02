namespace Lyo.Comic.Api.Models.Response;

/// <summary>Request body for retrieving multiple files by their IDs.</summary>
public sealed record FilesBatchReq(IReadOnlyList<Guid> Ids);

/// <summary>A single file entry returned in a batch file retrieval response.</summary>
public sealed record FileBatchEntry(Guid Id, string ContentType, string Data);