using Lyo.Api.Models.Enums;

namespace Lyo.Api.Tests;

public class ExportFormatExtensionsTests
{
    [Theory]
    [InlineData(ExportFormat.Csv, "text/csv")]
    [InlineData(ExportFormat.Xlsx, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData(ExportFormat.Json, "application/json")]
    [InlineData(ExportFormat.Yaml, "application/yaml")]
    [InlineData(ExportFormat.Xml, "application/xml")]
    [InlineData(ExportFormat.Toml, "application/toml")]
    public void FileType_ReturnsRealMimeType(ExportFormat format, string expected) => Assert.Equal(expected, format.FileType.MimeType);

    [Theory]
    [InlineData(ExportFormat.Csv, "export.csv")]
    [InlineData(ExportFormat.Xlsx, "export.xlsx")]
    [InlineData(ExportFormat.Json, "export.json")]
    [InlineData(ExportFormat.Yaml, "export.yaml")]
    [InlineData(ExportFormat.Xml, "export.xml")]
    [InlineData(ExportFormat.Toml, "export.toml")]
    public void DefaultFileName_UsesCatalogExtension(ExportFormat format, string expected) => Assert.Equal(expected, format.DefaultFileName);

    [Fact]
    public void FileType_YamlXmlAndToml_MapToCatalog()
    {
        Assert.Equal("application/yaml", ExportFormat.Yaml.FileType.MimeType);
        Assert.Contains("text/yaml", ExportFormat.Yaml.FileType.MimeTypeAliases);
        Assert.Contains(".yml", ExportFormat.Yaml.FileType.Aliases);
        Assert.Equal("export.yaml", ExportFormat.Yaml.DefaultFileName);
        Assert.Equal("application/xml", ExportFormat.Xml.FileType.MimeType);
        Assert.Equal("export.xml", ExportFormat.Xml.DefaultFileName);
        Assert.Equal("application/toml", ExportFormat.Toml.FileType.MimeType);
        Assert.Equal("export.toml", ExportFormat.Toml.DefaultFileName);
    }

    [Fact]
    public void FileType_UnknownFormat_FallsBackToOctetStream() => Assert.Equal("application/octet-stream", ((ExportFormat)999).FileType.MimeType);
}