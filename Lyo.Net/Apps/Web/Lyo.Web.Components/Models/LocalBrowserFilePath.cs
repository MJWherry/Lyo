using System.Diagnostics;

namespace Lyo.Web.Components.Models;

/// <summary>File streamed to a temp path on disk after a browser pick (no in-memory buffer).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record LocalBrowserFilePath(string FileName, string FilePath, long FileSize);