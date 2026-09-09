using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;

namespace Lyo.Formatter.Tests;

public class FormatterLyoTypeTests
{
    [Fact]
    public void Template_IsNonClrCatalogType()
    {
        var info = FormatterLyoType.EnsureRegistered();
        Assert.Same(FormatterLyoType.Template, info);
        Assert.Equal("Lyo.Formatter.Template", info.FullName);
        Assert.False(info.IsClr);
        Assert.Equal(LyoTypeEditorKind.Formatter, info.EditorKind);
        Assert.True(FormatterLyoType.IsFormattable(info));
        Assert.True(FormatterLyoType.IsFormattable(LyoTypeInfo.String));
        Assert.False(FormatterLyoType.IsFormattable(LyoTypeInfo.DateTime));
        Assert.True(FormatterLyoType.IsFormattableName("string"));
        Assert.Equal(typeof(string), info.Type);
        Assert.Contains(info, LyoTypeInfo.All);
    }

    [Fact]
    public void Template_DoesNotStealFromTypeString()
    {
        _ = FormatterLyoType.EnsureRegistered();
        Assert.Equal(LyoTypeInfo.String, LyoTypeInfo.FromType(typeof(string)));
        Assert.Same(FormatterLyoType.Template, LyoTypeInfo.FromName("formatter"));
        Assert.Same(FormatterLyoType.Template, LyoTypeInfo.FromName("Format"));
        Assert.Same(FormatterLyoType.Template, LyoTypeInfo.FromName(FormatterLyoType.FullName));
    }

    [Fact]
    public void TryValidateJson_AcceptsJsonStringOrRawTemplate()
    {
        _ = FormatterLyoType.EnsureRegistered();
        Assert.True(LyoTypeInfo.TryValidateJson("formatter", "\"{DateTime.UtcNow}\""));
        Assert.True(LyoTypeInfo.TryValidateJson("formatter", "{-}"));
        Assert.False(LyoTypeInfo.TryValidateJson("formatter", "not-a-template"));
        Assert.Equal("\"{DateTime.UtcNow}\"", FormatterLyoType.Template.ToJson("{DateTime.UtcNow}"));
    }
}
