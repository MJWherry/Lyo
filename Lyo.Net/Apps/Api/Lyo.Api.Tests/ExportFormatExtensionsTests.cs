using Lyo.Api.Models.Enums;

namespace Lyo.Api.Tests;

public class ExportFormatExtensionsTests
{
    [Theory]
    [InlineData(ExportFormat.Csv, "text/csv")]
    [InlineData(ExportFormat.Xlsx, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData(ExportFormat.Json, "application/json")]
    public void FileType_ReturnsRealMimeType(ExportFormat format, string expected) => Assert.Equal(expected, format.FileType.MimeType);

    [Theory]
    [InlineData(ExportFormat.Csv, "export.csv")]
    [InlineData(ExportFormat.Xlsx, "export.xlsx")]
    [InlineData(ExportFormat.Json, "export.json")]
    public void DefaultFileName_UsesCatalogExtension(ExportFormat format, string expected) => Assert.Equal(expected, format.DefaultFileName);

    [Fact]
    public void FileType_UnknownFormat_FallsBackToOctetStream() => Assert.Equal("application/octet-stream", ((ExportFormat)999).FileType.MimeType);
}
