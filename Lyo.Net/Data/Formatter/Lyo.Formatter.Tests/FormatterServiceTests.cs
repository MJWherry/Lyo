using System.Globalization;
using Lyo.IO.Temp.Models;
using Lyo.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartFormat;

namespace Lyo.Formatter.Tests;

public class FormatterServiceTests : IDisposable, IAsyncDisposable
{
    private readonly FormatterService _service;
    private readonly IOTempSession _tempSession;

    public FormatterServiceTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        var rootDir = Path.Combine(Path.GetTempPath(), "lyo-formatter-tests");
        Directory.CreateDirectory(rootDir);
        _tempSession = new(new() { RootDirectory = rootDir }, loggerFactory.CreateLogger<IOTempSession>());
        _service = new();
    }

    public async ValueTask DisposeAsync() => await _tempSession.DisposeAsync().ConfigureAwait(false);

    public void Dispose() => _tempSession.Dispose();

    [Fact]
    public void Format_SimplePlaceholder_ReplacesWithValue()
    {
        var result = _service.Format("Hello, {Name}!", new { Name = "World" });
        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public void Format_DictionaryContext_ReplacesPlaceholders()
    {
        var context = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["User"] = "Alice", ["Count"] = 42 };
        var result = _service.Format("User: {User}, Count: {Count}", context);
        Assert.Equal("User: Alice, Count: 42", result);
    }

    [Fact]
    public void Format_ActionBuilder_ReplacesPlaceholders()
    {
        var result = _service.Format("Hello, {Name}!", ctx => ctx.Add("Name", "Builder"));
        Assert.Equal("Hello, Builder!", result);
    }

    [Fact]
    public void Format_NullContext_DoesNotThrow()
    {
        var result = _service.Format("Static text", (object?)null);
        Assert.Equal("Static text", result);
    }

    [Fact]
    public void TryFormat_ValidTemplate_ReturnsTrueAndResult()
    {
        var success = _service.TryFormat("Hi {X}", new { X = "Y" }, out var result);
        Assert.True(success);
        Assert.Equal("Hi Y", result);
    }

    [Fact]
    public void TryFormat_InvalidTemplate_ReturnsFalse()
    {
        var success = _service.TryFormat("Hi {", new { }, out var result);
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void ValidateTemplate_ValidTemplate_ReturnsTrue() => Assert.True(_service.ValidateTemplate("Hello {Name}"));

    [Fact]
    public void TryValidateTemplate_InvalidTemplate_ReturnsFalseWithErrorMessage()
    {
        var valid = _service.TryValidateTemplate("Hello {", out var errorMessage);
        Assert.False(valid);
        Assert.NotNull(errorMessage);
    }

    [Fact]
    public void GetPlaceholders_ExtractsPlaceholderNames()
    {
        var placeholders = _service.GetPlaceholders("Hello {Name}, age {Age}");
        Assert.Contains("Name", placeholders);
        Assert.Contains("Age", placeholders);
        Assert.Equal(2, placeholders.Count);
    }

    [Fact]
    public void AllPlaceholdersResolved_FullyResolved_ReturnsTrue()
    {
        var output = _service.Format("Hi {X}", new { X = "Y" });
        Assert.True(_service.AllPlaceholdersResolved("Hi {X}", output));
    }

    [Fact]
    public void GetUnresolvedPlaceholders_UnresolvedPlaceholder_ReturnsIt()
    {
        var unresolved = _service.GetUnresolvedPlaceholders("Hi {X}", "Hi {X}");
        Assert.Contains("X", unresolved);
    }

    [Fact]
    public void CreateTemplate_WithContext_FormatsCorrectly()
    {
        var template = _service.CreateTemplate("Hello, {Name}!").WithValue("Name", "World");
        var result = template.Format();
        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public async Task Format_TemplateFromIOTempFile_Works()
    {
        var templateContent = "Report for {User} at {Date}";
        var tempPath = await _tempSession.CreateFileAsync(templateContent, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var contentFromFile = await File.ReadAllTextAsync(tempPath, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var result = _service.Format(contentFromFile, new { User = "Admin", Date = "2026-02-16" });
        Assert.Equal("Report for Admin at 2026-02-16", result);
    }

    [Fact]
    public void TryValidateContext_AllPlaceholdersSatisfied_ReturnsTrue()
    {
        var template = _service.CreateTemplate("Hello, {Name}!").WithValue("Name", "World");
        var valid = template.TryValidateContext(out var errorMessage);
        Assert.True(valid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void TryValidateContext_MissingPlaceholder_ReturnsFalseWithMessage()
    {
        var template = _service.CreateTemplate("Hello, {Name} and {Age}!").WithValue("Name", "World");
        var valid = template.TryValidateContext(out var errorMessage);
        Assert.False(valid);
        Assert.NotNull(errorMessage);
        Assert.Contains("Age", errorMessage);
    }

    [Fact]
    public void TryValidateContext_NoPlaceholders_ReturnsTrue()
    {
        var template = _service.CreateTemplate("Static text").WithValue("X", "ignored");
        var valid = template.TryValidateContext(out var errorMessage);
        Assert.True(valid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void WithContext_AnonymousObject_FormatsCorrectly()
    {
        var template = _service.CreateTemplate("Hi {User}").WithContext(new { User = "Alice" });
        Assert.Equal("Hi Alice", template.Format());
    }

    [Fact]
    public void WithContext_Dictionary_FormatsCorrectly()
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["X"] = 42, ["Y"] = "test" };
        var template = _service.CreateTemplate("{X} and {Y}").WithContext(dict);
        Assert.Equal("42 and test", template.Format());
    }

    [Fact]
    public void AddContext_WithAddIf_AddsWhenConditionTrue()
    {
        var template = _service.CreateTemplate("Value: {X}").AddContext(ctx => ctx.AddIf("X", "present", true));
        Assert.Equal("Value: present", template.Format());
    }

    [Fact]
    public void AddContext_WithAddIf_SkipsWhenConditionFalse()
    {
        var template = _service.CreateTemplate("Value: {X}").AddContext(ctx => ctx.AddIf("X", "present", false));
        var result = template.Format();
        Assert.Contains("{X}", result);
    }

    [Fact]
    public void AddContext_WithAddIf_FormatString_AppliesFormatWhenTrue()
    {
        var template = _service.CreateTemplate("Date: {D}").AddContext(ctx => ctx.AddIf("D", new DateTime(2026, 2, 28), "yyyy-MM-dd", true));
        Assert.Equal("Date: 2026-02-28", template.Format());
    }

    [Fact]
    public void AddContext_WithAddWhen_PredicateTrue_AddsValue()
    {
        var template = _service.CreateTemplate("Count: {C}").AddContext(ctx => ctx.AddWhen("C", 5, x => (int?)x! > 0));
        Assert.Equal("Count: 5", template.Format());
    }

    [Fact]
    public void AddContext_WithAddWhen_PredicateFalse_SkipsValue()
    {
        var template = _service.CreateTemplate("Count: {C}").AddContext(ctx => ctx.AddWhen("C", 0, x => (int?)x! > 0));
        var result = template.Format();
        Assert.Contains("{C}", result);
    }

    [Fact]
    public void AddContext_WithAdd_FormatString_FormatsValue()
    {
        _service.Culture = CultureInfo.InvariantCulture;
        var template = _service.CreateTemplate("Amount: {A}").AddContext(ctx => ctx.Add("A", 1234.56m, "N2"));
        Assert.Equal("Amount: 1,234.56", template.Format());
    }

    [Fact]
    public void AddContext_WithAdd_FormatterFunc_UsesCustomFormatter()
    {
        var template = _service.CreateTemplate("Custom: {V}").AddContext(ctx => ctx.Add("V", 42, v => $"#{v}"));
        Assert.Equal("Custom: #42", template.Format());
    }

    [Fact]
    public void AddContext_WithAddTyped_FormatterFunc_UsesTypedFormatter()
    {
        var template = _service.CreateTemplate("Id: {Id}").AddContext(ctx => ctx.Add("Id", Guid.Parse("11111111-1111-1111-1111-111111111111"), g => g.ToString("N")[..8]));
        Assert.Equal("Id: 11111111", template.Format());
    }

    [Fact]
    public void Format_WithAdditionalContext_MergesContext()
    {
        var template = _service.CreateTemplate("Hello {A} and {B}").WithValue("A", "first");
        var additional = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["B"] = "second" };
        var result = template.Format(additional);
        Assert.Contains("first", result);
        Assert.Contains("second", result);
        Assert.DoesNotContain("{A}", result);
        Assert.DoesNotContain("{B}", result);
    }

    [Fact]
    public void Format_NoContext_ReturnsTemplateWithPlaceholders()
    {
        var template = _service.CreateTemplate("Hi {X}");
        var result = template.Format();
        Assert.Contains("{X}", result);
    }

    [Fact]
    public void AddFormatterService_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddFormatterService();
        var provider = services.BuildServiceProvider();
        var formatter = provider.GetRequiredService<IFormatterService>();
        Assert.NotNull(formatter);
        Assert.Equal("Hello World", formatter.Format("Hello {Name}", new { Name = "World" }));
    }

    [Fact]
    public void AddFormatterService_WithCustomFormatterFactory_UsesCustomFormatter()
    {
        var services = new ServiceCollection();
        services.AddFormatterService(_ => {
            var sf = Smart.CreateDefaultSmartFormat();
            return sf;
        });

        var provider = services.BuildServiceProvider();
        var formatter = provider.GetRequiredService<IFormatterService>();
        Assert.NotNull(formatter);
        Assert.Equal("Hello World", formatter.Format("Hello {Name}", new { Name = "World" }));
    }
}