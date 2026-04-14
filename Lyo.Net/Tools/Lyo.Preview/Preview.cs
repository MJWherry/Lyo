using Lyo.Common.Records;

namespace Lyo.Preview;

/// <summary>Static convenience methods for previewing without dependency injection.</summary>
public static class Preview
{
    private static IPreviewService? _default;

    /// <summary>Gets or creates the default preview service instance.</summary>
    public static IPreviewService Default => _default ??= new BrowserPreview();

    /// <summary>Previews a file (local path or URL). Type inferred from extension.</summary>
    public static Task<string?> FileAsync(string pathOrUrl, CancellationToken ct = default) => Default.PreviewFileAsync(pathOrUrl, ct);

    /// <summary>Previews content from a stream. FileTypeInfo required.</summary>
    public static Task<string?> Async(Stream stream, FileTypeInfo fileType, CancellationToken ct = default) => Default.PreviewAsync(stream, fileType, ct);

    /// <summary>Previews content from bytes. FileTypeInfo required.</summary>
    public static Task<string?> Async(byte[] bytes, FileTypeInfo fileType, CancellationToken ct = default) => Default.PreviewAsync(bytes, fileType, ct);

    /// <summary>Resets the default service (e.g. for testing).</summary>
    public static void ResetDefault() => _default = null;
}