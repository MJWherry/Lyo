using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>JSON body posted to <c>files/physical/move</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record MovePhysicalObjectRequest(string PhysicalKey, string? DestinationPathPrefix = null)
{
    /// <inheritdoc />
    public override string ToString() => $"MovePhysicalObjectRequest: PhysicalKey={PhysicalKey}, DestinationPathPrefix={DestinationPathPrefix ?? "(root)"}";
}

/// <summary>JSON body posted to <c>files/physical/copy</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record CopyPhysicalObjectRequest(string PhysicalKey, string? DestinationPathPrefix = null)
{
    /// <inheritdoc />
    public override string ToString() => $"CopyPhysicalObjectRequest: PhysicalKey={PhysicalKey}, DestinationPathPrefix={DestinationPathPrefix ?? "(root)"}";
}

/// <summary>JSON body posted to <c>files/physical/rename</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record RenamePhysicalObjectRequest(string PhysicalKey, string NewFileName)
{
    /// <inheritdoc />
    public override string ToString() => $"RenamePhysicalObjectRequest: PhysicalKey={PhysicalKey}, NewFileName={NewFileName}";
}

/// <summary>New or remaining physical key after a raw-object mutation.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record PhysicalObjectResult(string PhysicalKey)
{
    /// <inheritdoc />
    public override string ToString() => $"PhysicalObjectResult: PhysicalKey={PhysicalKey}";
}