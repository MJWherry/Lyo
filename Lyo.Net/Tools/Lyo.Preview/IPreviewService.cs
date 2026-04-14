using Lyo.Common.Records;

namespace Lyo.Preview;

/// <summary>Service for previewing content in the system default browser.</summary>
public interface IPreviewService
{
    /// <summary>Previews a file (local path or URL). Type inferred from path/extension.</summary>
    /// <param name="pathOrUrl">Local file path or URL (http/https). Type determined from extension.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The URL that was opened, or null if preview failed.</returns>
    Task<string?> PreviewFileAsync(string pathOrUrl, CancellationToken ct = default);

    /// <summary>Previews content from a stream. Caller must specify type since it cannot be inferred.</summary>
    /// <param name="stream">Content stream.</param>
    /// <param name="fileType">File type for MIME (required).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<string?> PreviewAsync(Stream stream, FileTypeInfo fileType, CancellationToken ct = default);

    /// <summary>Previews content from bytes. Caller must specify type since it cannot be inferred.</summary>
    /// <param name="bytes">Content bytes.</param>
    /// <param name="fileType">File type for MIME (required).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<string?> PreviewAsync(byte[] bytes, FileTypeInfo fileType, CancellationToken ct = default);
}