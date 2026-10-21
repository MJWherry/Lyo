using Lyo.Api.Import.Toml;
using Lyo.Api.Import.Xml;
using Lyo.Api.Import.Yaml;
using Lyo.Api.Models.Enums;

namespace Lyo.Api.Import.Tests;

public class TextFormatImportTests
{
    [Fact]
    public void Import_Yaml_BadItem_ReturnsTwoRowsAndOneIssue()
    {
        const string yaml = """
                            - firstName: Ada
                              age: 1
                            - nope
                            - firstName: Bea
                              age: 2
                            """;

        var result = new ImportService([new YamlImportFormatHandler()]).Import<Person>(new() { Format = ImportFormat.Yaml, Content = yaml });
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Rows.Count);
        Assert.Single(result.Issues);
    }

    [Fact]
    public void Import_Yaml_Map_RenamesProperty()
    {
        const string yaml = """
                            - given: Ada
                              years: 4
                            """;

        var result = new ImportService([new YamlImportFormatHandler()]).Import<Person>(
            new() { Format = ImportFormat.Yaml, Content = yaml, Fields = [new() { Source = "given", Target = "FirstName" }, new() { Source = "years", Target = "Age" }] });

        Assert.True(result.IsSuccess);
        Assert.Equal("Ada", result.Rows[0].FirstName);
        Assert.Equal(4, result.Rows[0].Age);
    }

    [Fact]
    public void Import_Yaml_RootMappingWithoutRowsPath_IsDocumentError()
    {
        const string yaml = """
                            firstName: Ada
                            age: 1
                            """;

        var result = new ImportService([new YamlImportFormatHandler()]).Import<Person>(new() { Format = ImportFormat.Yaml, Content = yaml });
        Assert.False(result.IsSuccess);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void Import_Xml_BadItem_ReturnsTwoRowsAndOneIssue()
    {
        const string xml = """
                           <rows>
                             <item><firstName>Ada</firstName><age>1</age></item>
                             <item>nope</item>
                             <item><firstName>Bea</firstName><age>2</age></item>
                           </rows>
                           """;

        var result = new ImportService([new XmlImportFormatHandler()]).Import<Person>(new() { Format = ImportFormat.Xml, Content = xml });
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Rows.Count);
        Assert.Single(result.Issues);
    }

    [Fact]
    public void Import_Xml_RootObjectWithoutRowsPath_IsDocumentError()
    {
        const string xml = "<person><firstName>Ada</firstName><age>1</age></person>";
        var result = new ImportService([new XmlImportFormatHandler()]).Import<Person>(new() { Format = ImportFormat.Xml, Content = xml });
        Assert.False(result.IsSuccess);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void Import_Toml_BadItem_ReturnsTwoRowsAndOneIssue()
    {
        const string toml = """
                            [[items]]
                            firstName = "Ada"
                            age = 1

                            [[items]]
                            firstName = "Bad"
                            age = "nope"

                            [[items]]
                            firstName = "Bea"
                            age = 2
                            """;

        var result = new ImportService([new TomlImportFormatHandler()]).Import<Person>(new() { Format = ImportFormat.Toml, Content = toml, RowsPath = "items" });
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Rows.Count);
        Assert.Single(result.Issues);
    }

    [Fact]
    public void Import_Toml_RootTableWithoutRowsPath_IsDocumentError()
    {
        const string toml = """
                            firstName = "Ada"
                            age = 1
                            """;

        var result = new ImportService([new TomlImportFormatHandler()]).Import<Person>(new() { Format = ImportFormat.Toml, Content = toml });
        Assert.False(result.IsSuccess);
        Assert.Empty(result.Rows);
    }

    private sealed class Person
    {
        public string? FirstName { get; set; }

        public int Age { get; set; }
    }
}