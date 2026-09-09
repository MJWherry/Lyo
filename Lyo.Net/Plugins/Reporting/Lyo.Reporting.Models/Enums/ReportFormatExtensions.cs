using Lyo.Common.Metadata.Records;

namespace Lyo.Reporting.Models.Enums;

/// <summary>
/// Maps <see cref="ReportFormat" /> onto the shared <see cref="FileTypeInfo" /> catalog so renderers and the report service never hardcode a MIME type or extension.
/// </summary>
public static class ReportFormatExtensions
{
    extension(ReportFormat format)
    {
        /// <summary>
        /// Catalog entry for this format, carrying the MIME type, extension, and aliases. Unrecognized values fall back to HTML, matching the renderer default.
        /// </summary>
        public FileTypeInfo FileType
            => format switch {
                ReportFormat.Pdf => FileTypeInfo.Pdf,
                ReportFormat.Csv => FileTypeInfo.Csv,
                ReportFormat.Xlsx => FileTypeInfo.Xlsx,
                ReportFormat.Json => FileTypeInfo.Json,
                var _ => FileTypeInfo.Html
            };

        /// <summary>Output file extension including the leading dot, such as <c>.xlsx</c>.</summary>
        public string Extension => format.FileType.DefaultExtension;

        /// <summary>HTTP <c>Content-Type</c> for the rendered output, with a UTF-8 charset on text formats only.</summary>
        public string ContentType => format.FileType.HttpContentType;
    }
}
