namespace Lyo.Web.WebRenderer.Models;

/// <summary>
/// HTML plus capture settings. A host can serialize this and hand it to <see cref="IWebRenderGenerator" />. PDF fields apply when <see cref="Format" /> is
/// <see cref="WebRenderFormat.Pdf" />. Image fields apply to PNG, JPEG, and WebP.
/// </summary>
public sealed class WebRenderReq
{
    /// <summary>HTML document to capture. Required for <see cref="IWebRenderGenerator.GenerateAsync" />.</summary>
    public string? Html { get; set; }

    /// <summary>Output file. Starts as PDF.</summary>
    public WebRenderFormat Format { get; set; } = WebRenderFormat.Pdf;

    /// <summary>Paper name: Letter, Legal, Tabloid, A3, A4, A5, or CSS. Null and CSS use the HTML <c>@page</c> size when <see cref="PreferCssPageSize" /> is true, and A4 otherwise.</summary>
    public string? Paper { get; set; }

    /// <summary>Print the PDF in landscape. Ignored for images.</summary>
    public bool Landscape { get; set; }

    /// <summary>PDF margin applied to every side, such as <c>1in</c>. Ignored for images.</summary>
    public string? Margin { get; set; }

    /// <summary>PDF scale between 0.1 and 2. Null keeps the browser default of 1.</summary>
    public double? Scale { get; set; }

    /// <summary>When true, a CSS <c>@page</c> size wins over <see cref="Paper" />. Starts true.</summary>
    public bool PreferCssPageSize { get; set; } = true;

    /// <summary>Capture the full scrollable page for an image. Starts true.</summary>
    public bool FullPage { get; set; } = true;

    /// <summary>Image viewport width in pixels. Null keeps the browser default.</summary>
    public int? ViewportWidth { get; set; }

    /// <summary>Image viewport height in pixels. Null keeps the browser default.</summary>
    public int? ViewportHeight { get; set; }

    /// <summary>JPEG or WebP quality from 0 to 100. Ignored for PNG and PDF.</summary>
    public int? Quality { get; set; }

    /// <summary>Omit the default white background so a PNG or WebP can be transparent.</summary>
    public bool OmitBackground { get; set; }

    /// <summary>Destination path for <see cref="IWebRenderGenerator.GenerateAsync" />.</summary>
    public string? OutputFilePath { get; set; }
}