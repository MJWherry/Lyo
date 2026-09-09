using Lyo.Formatter.Web.Components;

namespace Lyo.Formatter.Tests;

/// <summary>
/// A formatter-typed parameter value is stored as JSON, so the editor unwraps it for editing and re-serializes it on every keystroke. Getting that round-trip wrong
/// either marks an untouched definition dirty or writes a doubly-escaped template, so the test checks it directly.
/// </summary>
public class LyoFormatterValueEditorTests
{
    [Theory]
    [InlineData("Hello {Name}")]
    [InlineData("Path C:\\temp\\{Name}")]
    [InlineData("Quote \"{Name}\"")]
    [InlineData("Line one\nline two")]
    [InlineData("{client.contact.emailAddress}")]
    public void ToJson_ThenUnwrap_ReturnsTheOriginalTemplate(string template)
        => Assert.Equal(template, LyoFormatterValueEditor.UnwrapJson(LyoFormatterValueEditor.ToJson(template)));

    /// <summary>An unset value is null, not <c>""</c>, matching what every other editor writes so the required check treats it as empty.</summary>
    [Fact]
    public void ToJson_EmptyTemplate_ReturnsNull() => Assert.Null(LyoFormatterValueEditor.ToJson(""));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UnwrapJson_NoValue_ReturnsEmptyTemplate(string? json) => Assert.Equal("", LyoFormatterValueEditor.UnwrapJson(json));

    /// <summary>A value written before templates were JSON-serialized, or edited by hand, still has to open in the editor instead of showing its quotes.</summary>
    [Theory]
    [InlineData("Hello {Name}", "Hello {Name}")]
    [InlineData("\"Hello {Name}\"", "Hello {Name}")]
    public void UnwrapJson_RawTemplate_IsAccepted(string json, string expected) => Assert.Equal(expected, LyoFormatterValueEditor.UnwrapJson(json));

    /// <summary>Load then save with no edit must produce the same JSON; otherwise opening a definition marks it dirty.</summary>
    [Fact]
    public void UnwrapJson_ThenToJson_IsStableForAStoredValue()
    {
        const string stored = "\"Run of {Definition.Name}\"";

        Assert.Equal(stored, LyoFormatterValueEditor.ToJson(LyoFormatterValueEditor.UnwrapJson(stored)));
    }
}
