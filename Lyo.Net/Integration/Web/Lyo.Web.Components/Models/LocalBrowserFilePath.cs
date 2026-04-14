namespace Lyo.Web.Components.Models;

/// <summary>File streamed to a temp path on disk after browser pick (no in-memory buffer).</summary>
public sealed record LocalBrowserFilePath(string FileName, string FilePath, long FileSize);
