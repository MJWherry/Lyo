using Lyo.Common.Core.Extensions;
using Lyo.Common.Metadata.Extensions;
using Lyo.Common.Core.Pathing;
using Lyo.Common.Metadata.Records;
using Lyo.Common.Core.Security;

namespace Lyo.Common.Tests;

/// <summary>Covers the shared primitives call sites were previously hand-rolling: truncation, filename sanitization, content types, and random string generation.</summary>
public class CatalogPrimitiveTests
{
    [Theory]
    [InlineData("short", 10, "short")]
    [InlineData("exactlyten", 10, "exactlyten")]
    [InlineData("elevenchars", 10, "elevenc...")]
    [InlineData("abcdef", 3, "...")]
    [InlineData("abcdef", 2, "..")]
    public void TruncateWithEllipsis_SpendsBudgetOnSuffix(string value, int maxLength, string expected)
        => Assert.Equal(expected, value.TruncateWithEllipsis(maxLength));

    [Fact]
    public void TruncateWithEllipsis_NullEmptyOrZeroBudget_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ((string?)null).TruncateWithEllipsis(10));
        Assert.Equal(string.Empty, "".TruncateWithEllipsis(10));
        Assert.Equal(string.Empty, "value".TruncateWithEllipsis(0));
    }

    [Fact]
    public void TruncateWithEllipsis_CustomSuffix_IsUsed() => Assert.Equal("abcd…", "abcdefgh".TruncateWithEllipsis(5, "…"));

    [Fact]
    public void SanitizeFileName_DropsInvalidCharsWhenReplacementIsNull() => Assert.Equal("report2024.csv", PathHelpers.SanitizeFileName("report/2024.csv", null));

    [Fact]
    public void SanitizeFileName_TakeLeaf_CollapsesPathToFileName() => Assert.Equal("report.csv", PathHelpers.SanitizeFileName("/var/tmp/report.csv", takeLeaf: true));

    [Fact]
    public void SanitizeFileName_StripControlCharacters_RemovesNewlines()
        => Assert.Equal("re_port.csv", PathHelpers.SanitizeFileName("re\nport.csv", stripControlCharacters: true));

    [Fact]
    public void SanitizeFileName_MaxLength_PreservesExtension()
    {
        var result = PathHelpers.SanitizeFileName("a-very-long-report-name.csv", maxLength: 12);
        Assert.Equal(12, result!.Length);
        Assert.EndsWith(".csv", result);
    }

    [Fact]
    public void SanitizeFileName_NothingUsable_ReturnsNull() => Assert.Null(PathHelpers.SanitizeFileName("///", null));

    [Fact]
    public void ContentType_AppendsUtf8CharsetByDefault() => Assert.Equal("text/csv; charset=utf-8", FileTypeInfo.Csv.ContentType());

    [Fact]
    public void ContentType_BlankCharset_ReturnsBareMimeType() => Assert.Equal("text/csv", FileTypeInfo.Csv.ContentType("  "));

    [Fact]
    public void ProblemJson_IsTheRfc9457MediaType() => Assert.Equal("application/problem+json", FileTypeInfo.ProblemJson.MimeType);

    [Fact]
    public void Avif_ResolvesFromExtension() => Assert.Equal(FileTypeInfo.Avif, FileTypeInfo.FromExtension(".avif"));

    [Fact]
    public void GetString_UsesOnlyTheSuppliedAlphabet()
    {
        const string alphabet = "abc";
        var value = CryptographicRandom.GetString(256, alphabet);
        Assert.Equal(256, value.Length);
        Assert.All(value, c => Assert.Contains(c, alphabet));
    }

    [Fact]
    public void GetString_ZeroLength_ReturnsEmpty() => Assert.Equal(string.Empty, CryptographicRandom.GetString(0, "abc"));

    [Fact]
    public void GetString_SingleCharacterAlphabet_Repeats() => Assert.Equal("xxxx", CryptographicRandom.GetString(4, "x"));

    [Fact]
    public void GetString_RejectsEmptyAlphabetAndNegativeLength()
    {
        Assert.ThrowsAny<ArgumentException>(() => CryptographicRandom.GetString(4, ""));
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => CryptographicRandom.GetString(-1, "abc"));
    }
}
