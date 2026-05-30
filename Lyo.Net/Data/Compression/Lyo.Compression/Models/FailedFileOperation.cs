using System.Diagnostics;

namespace Lyo.Compression.Models;

/// <summary>Describes a single file that failed during a batch operation.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FailedFileOperation(string FilePath, string ErrorMessage)
{
    public override string ToString() => $"File='{FilePath}' Error='{ErrorMessage}'";
}