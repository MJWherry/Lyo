using System.Reflection;
using Lyo.Api.Export.Toml;
using Lyo.Api.Export.Xml;
using Lyo.Api.Export.Yaml;
using Lyo.Api.Services.Export;
using Lyo.Common.Core.Enums;
using Lyo.Json;
using Lyo.Toml;
using Lyo.Xml;
using Lyo.Yaml;

namespace Lyo.Api.Tests.Services.Export;

public class YamlExportFormatHandlerTests
{
    [Fact]
    public async Task WriteTypedAsync_People_EmitsDocument()
    {
        var (stream, contentType, fileName) = await new YamlExportFormatHandler(new YamlService()).WriteTypedAsync<Person>([new("Ada"), new("Grace")], null, TestCt);
        await using (stream) {
            var text = await new StreamReader(stream).ReadToEndAsync(TestCt);
            Assert.Equal("application/yaml", contentType);
            Assert.Equal("export.yaml", fileName);
            Assert.Contains("Ada", text, StringComparison.Ordinal);
            Assert.Contains("Grace", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task WriteProjectedAsync_Formatters_KeepsCallerOrder()
    {
        var service = new YamlService(o => o.Order = MemberOrder.Alphabetical);
        var (stream, _, _) = await new YamlExportFormatHandler(service).WriteProjectedAsync(FormattedExportRows.Rows(), FormattedExportRows.Formatters(), TestCt);
        await using (stream) {
            var text = await new StreamReader(stream).ReadToEndAsync(TestCt);
            Assert.True(text.IndexOf("note:", StringComparison.Ordinal) < text.IndexOf("fullName:", StringComparison.Ordinal));
            Assert.Contains("Ada Lovelace", text, StringComparison.Ordinal);
        }
    }

    private sealed record Person(string GivenName);
}

public class XmlExportFormatHandlerTests
{
    [Fact]
    public async Task WriteTypedAsync_People_EmitsDocument()
    {
        var (stream, contentType, fileName) = await new XmlExportFormatHandler(new XmlService()).WriteTypedAsync<Person>([new("Ada"), new("Grace")], null, TestCt);
        await using (stream) {
            var text = await new StreamReader(stream).ReadToEndAsync(TestCt);
            Assert.Equal("application/xml", contentType);
            Assert.Equal("export.xml", fileName);
            Assert.Contains("Ada", text, StringComparison.Ordinal);
            Assert.Contains("Grace", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task WriteProjectedAsync_Formatters_KeepsCallerOrder()
    {
        var service = new XmlService(o => o.Order = MemberOrder.Alphabetical);
        var (stream, _, _) = await new XmlExportFormatHandler(service).WriteProjectedAsync(FormattedExportRows.Rows(), FormattedExportRows.Formatters(), TestCt);
        await using (stream) {
            var text = await new StreamReader(stream).ReadToEndAsync(TestCt);
            Assert.True(text.IndexOf("<note>", StringComparison.Ordinal) < text.IndexOf("<fullName>", StringComparison.Ordinal));
            Assert.Contains("Ada Lovelace", text, StringComparison.Ordinal);
        }
    }

    private sealed record Person(string GivenName);
}

public class TomlExportFormatHandlerTests
{
    [Fact]
    public async Task WriteTypedAsync_People_EmitsDocument()
    {
        var (stream, contentType, fileName) = await new TomlExportFormatHandler(new TomlService()).WriteTypedAsync<Person>([new("Ada"), new("Grace")], null, TestCt);
        await using (stream) {
            var text = await new StreamReader(stream).ReadToEndAsync(TestCt);
            Assert.Equal("application/toml", contentType);
            Assert.Equal("export.toml", fileName);
            Assert.Contains("Ada", text, StringComparison.Ordinal);
            Assert.Contains("Grace", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task WriteProjectedAsync_Formatters_KeepsCallerOrder()
    {
        var service = new TomlService(o => o.Order = MemberOrder.Alphabetical);
        var (stream, _, _) = await new TomlExportFormatHandler(service).WriteProjectedAsync(FormattedExportRows.Rows(), FormattedExportRows.Formatters(), TestCt);
        await using (stream) {
            var text = await new StreamReader(stream).ReadToEndAsync(TestCt);
            Assert.True(text.IndexOf("note", StringComparison.Ordinal) < text.IndexOf("fullName", StringComparison.Ordinal));
            Assert.Contains("Ada Lovelace", text, StringComparison.Ordinal);
        }
    }

    private sealed record Person(string GivenName);
}

public class JsonExportFormatHandlerTests
{
    [Fact]
    public async Task WriteProjectedAsync_Formatters_KeepsCallerOrder()
    {
        var service = new JsonService(o => o.Order = MemberOrder.Alphabetical);
        var (stream, _, _) = await new JsonExportFormatHandler(service).WriteProjectedAsync(FormattedExportRows.Rows(), FormattedExportRows.Formatters(), TestCt);
        await using (stream) {
            var text = await new StreamReader(stream).ReadToEndAsync(TestCt);
            Assert.True(text.IndexOf("\"note\"", StringComparison.Ordinal) < text.IndexOf("\"fullName\"", StringComparison.Ordinal));
            Assert.Contains("Ada Lovelace", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task WriteTypedAsync_Columns_KeepsHeaderOrderAndDropsUnlistedProperties()
    {
        var columns = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal) { ["label"] = typeof(Person).GetProperty(nameof(Person.GivenName))! };
        var service = new JsonService(o => o.Order = MemberOrder.Alphabetical);
        var (stream, _, _) = await new JsonExportFormatHandler(service).WriteTypedAsync<Person>([new("Ada", 36)], columns, TestCt);
        await using (stream) {
            var text = await new StreamReader(stream).ReadToEndAsync(TestCt);
            Assert.Contains("\"label\":\"Ada\"", text, StringComparison.Ordinal);
            Assert.DoesNotContain("36", text, StringComparison.Ordinal);
        }
    }

    private sealed record Person(string GivenName, int Age);
}

file static class FormattedExportRows
{
    public static object?[] Rows() => [new PersonRow("Ada", "Lovelace")];

    public static Dictionary<string, Func<object?, string>> Formatters()
        => new(StringComparer.Ordinal) { ["note"] = _ => "hi", ["fullName"] = row => row is PersonRow person ? person.GivenName + " " + person.FamilyName : "" };

    private sealed record PersonRow(string GivenName, string FamilyName);
}