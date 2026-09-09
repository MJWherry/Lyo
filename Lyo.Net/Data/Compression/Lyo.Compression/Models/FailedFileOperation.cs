using System.Diagnostics;

namespace Lyo.Compression.Models;

/// <summary>One file that failed during a batch operation.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FailedFileOperation(string FilePath, string ErrorMessage)
{
    public override string ToString() => $"File='{FilePath}' Error='{ErrorMessage}'";
}