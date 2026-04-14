using System.Diagnostics;

namespace Lyo.Compression.Models;

[DebuggerDisplay("{ToString()}")]
public sealed record FailedFileOperation(string FilePath, string ErrorMessage)
{
    public override string ToString() => $"File='{FilePath}' Error='{ErrorMessage}'";
}