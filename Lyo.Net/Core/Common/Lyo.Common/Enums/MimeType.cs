using System.ComponentModel;

namespace Lyo.Common.Enums;

/// <summary>Represents standard MIME types.</summary>
public enum MimeType
{
    /// <summary>application/octet-stream (Unknown MIME type)</summary>
    [Description("application/octet-stream")]
    Unknown = 0,

    // 📄 Document Formats

    /// <summary>application/pdf (Adobe PDF document)</summary>
    [Description("application/pdf")]
    Pdf = 1,

    /// <summary>application/msword (Microsoft Word document)</summary>
    [Description("application/msword")]
    Doc = 2,

    /// <summary>application/vnd.openxmlformats-officedocument.wordprocessingml.document (Microsoft Word Open XML document)</summary>
    [Description("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    Docx = 3,

    /// <summary>application/vnd.ms-excel (Microsoft Excel spreadsheet)</summary>
    [Description("application/vnd.ms-excel")]
    Xls = 4,

    /// <summary>application/vnd.openxmlformats-officedocument.spreadsheetml.sheet (Microsoft Excel Open XML spreadsheet)</summary>
    [Description("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    Xlsx = 5,

    // 🧾 Text & Data

    /// <summary>text/csv (Comma-separated values)</summary>
    [Description("text/csv")]
    Csv = 6,

    /// <summary>text/plain (Plain text)</summary>
    [Description("text/plain")]
    Txt = 7,

    /// <summary>application/x-tex (LaTeX document)</summary>
    [Description("application/x-tex")]
    Tex = 8,

    /// <summary>application/json (JSON data)</summary>
    [Description("application/json")]
    Json = 9,

    /// <summary>application/xml (XML data)</summary>
    [Description("application/xml")]
    Xml = 10,

    // 🌐 Web

    /// <summary>text/html (HTML document)</summary>
    [Description("text/html")]
    Html = 11,

    // 🧱 Binary

    /// <summary>application/octet-stream (Binary data)</summary>
    [Description("application/octet-stream")]
    Bin = 12,

    /// <summary>application/x-dump (Data dump)</summary>
    [Description("application/x-dump")]
    Dump = 13,

    // 🖼️ Images

    /// <summary>image/jpeg (JPEG image (.jpg))</summary>
    [Description("image/jpeg")]
    Jpg = 14,

    /// <summary>image/jpeg (JPEG image (.jpeg))</summary>
    [Description("image/jpeg")]
    Jpeg = 15,

    /// <summary>image/png (PNG image)</summary>
    [Description("image/png")]
    Png = 16,

    /// <summary>image/gif (GIF image)</summary>
    [Description("image/gif")]
    Gif = 17,

    /// <summary>image/bmp (BMP image)</summary>
    [Description("image/bmp")]
    Bmp = 18,

    /// <summary>image/svg+xml (SVG image)</summary>
    [Description("image/svg+xml")]
    Svg = 19,

    /// <summary>image/tiff (TIFF image (.tif))</summary>
    [Description("image/tiff")]
    Tif = 20,

    /// <summary>image/tiff (TIFF image (.tiff))</summary>
    [Description("image/tiff")]
    Tiff = 21,

    /// <summary>image/webp (WebP image)</summary>
    [Description("image/webp")]
    Webp = 22,

    /// <summary>image/vnd.microsoft.icon (Windows icon .ico)</summary>
    [Description("image/vnd.microsoft.icon")]
    Ico = 23
}