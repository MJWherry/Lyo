using Lyo.Common.Metadata.Records;

namespace Lyo.Api.Models.Enums;

/// <summary>Maps <see cref="ExportFormat" /> onto the shared <see cref="FileTypeInfo" /> catalog so MIME types and extensions are not hardcoded at each call site.</summary>
public static class ExportFormatExtensions
{
    extension(ExportFormat format)
    {
        /// <summary>Catalog entry for this export format: MIME type, extension, and display name.</summary>
        public FileTypeInfo FileType
            => format switch {
                ExportFormat.Csv => FileTypeInfo.Csv,
                ExportFormat.Xlsx => FileTypeInfo.Xlsx,
                ExportFormat.Json => FileTypeInfo.Json,
                var _ => FileTypeInfo.Unknown
            };

        /// <summary>Default download file name for this format, such as <c>export.csv</c>.</summary>
        public string DefaultFileName => "export" + format.FileType.DefaultExtension;
    }
}
