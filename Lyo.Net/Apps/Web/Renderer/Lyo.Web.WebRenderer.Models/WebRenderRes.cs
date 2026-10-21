namespace Lyo.Web.WebRenderer.Models;

/// <summary>Result of writing a capture to <see cref="OutputPath" />.</summary>
public sealed class WebRenderRes
{
    /// <summary>True when the file was written.</summary>
    public bool Success { get; set; }

    /// <summary>MIME type of the file, such as <c>application/pdf</c>.</summary>
    public string? ContentType { get; set; }

    /// <summary>File name without a directory.</summary>
    public string? FileName { get; set; }

    /// <summary>Full path of the written file.</summary>
    public string? OutputPath { get; set; }

    /// <summary>Size of the written file in bytes.</summary>
    public long ByteLength { get; set; }

    /// <summary>Failure text when <see cref="Success" /> is false.</summary>
    public string? Error { get; set; }
}