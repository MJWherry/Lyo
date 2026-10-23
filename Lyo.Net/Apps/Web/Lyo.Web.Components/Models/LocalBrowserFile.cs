using System.Diagnostics;

namespace Lyo.Web.Components.Models;

/// <summary>File bytes read in the browser after pick (no upload to the server).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record LocalBrowserFile(string FileName, byte[] Content)
{
    /// <summary>File name and content length. Bytes are omitted.</summary>
    public override string ToString() => $"{FileName} ({Content?.Length ?? 0} bytes)";
}