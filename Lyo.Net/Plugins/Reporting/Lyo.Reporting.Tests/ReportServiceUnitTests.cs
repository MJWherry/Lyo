using System.Text.Json;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions.Models;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Providers;
using Lyo.Reporting.Postgres;
using Lyo.Reporting.Postgres.Database;
namespace Lyo.Reporting.Tests;

public sealed class ReportServiceUnitTests
{
    [Fact]
    public void Constructor_DuplicateProviderKeys_FailsWithActionableMessage()
    {
        var ex = Assert.Throws<ConflictException>(() => new ReportService(
            null!, [], [new FakeProvider("dup-key"), new FakeProvider("DUP-KEY")], [], null!, new PostgresReportingOptions { ConnectionString = "x" }, null!));

        Assert.Contains("dup-key", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("unique", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_DuplicateProfileKeys_FailsWithActionableMessage()
    {
        var ex = Assert.Throws<ConflictException>(() => new ReportService(
            null!, [], [], [new() { Key = "profile-a" }, new() { Key = "Profile-A" }], null!, new PostgresReportingOptions { ConnectionString = "x" }, null!));

        Assert.Contains("profile-a", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("report.csv", "report.csv")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData(@"..\..\windows\evil.csv", "evil.csv")]
    [InlineData("sub/dir/name.csv", "name.csv")]
    [InlineData("  spaced.csv  ", "spaced.csv")]
    [InlineData("trailing-dots...", "trailing-dots")]
    [InlineData("...", null)]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void SanitizeFileName_PathsAndInvalidInput_Strips(string? input, string? expected) => Assert.Equal(expected, ReportService.SanitizeFileName(input));

    [Fact]
    public void SanitizeFileName_InvalidAndControlChars_Removes()
    {
        var sanitized = ReportService.SanitizeFileName("re\0po\trt.csv");
        Assert.Equal("report.csv", sanitized);
    }

    [Fact]
    public void SanitizeFileName_LongName_CapsLengthPreservingExtension()
    {
        var sanitized = ReportService.SanitizeFileName(new string('a', 400) + ".csv");
        Assert.NotNull(sanitized);
        Assert.True(sanitized!.Length <= ReportService.MaxFileNameLength);
        Assert.EndsWith(".csv", sanitized, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeParametersJson_MultiValues_PreservesAsArrays()
    {
        var json = ReportService.SerializeParametersJson(
            [new("Tag", LyoTypeInfo.StringList, """["a","b"]"""), new("Single", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("only"))]);

        using var doc = JsonDocument.Parse(json);
        var tag = doc.RootElement.GetProperty("Tag");
        Assert.Equal(JsonValueKind.Array, tag.ValueKind);
        Assert.Equal(["a", "b"], tag.EnumerateArray().Select(e => e.GetString()!).ToArray());
        Assert.Equal("only", doc.RootElement.GetProperty("Single").GetString());
    }

    [Fact]
    public void MergeParameters_Collection_KeepsOneJsonArrayRow()
    {
        var def = new List<ReportDefinitionParameter> { new() { Key = "Tag", Type = LyoTypeInfo.StringList.FullName } };
        var merged = ReportService.MergeParameters(def, [new("Tag", LyoTypeInfo.StringList, """["a","b"]""")]);
        Assert.Equal(1, merged.Count(p => p.Key == "Tag"));
        Assert.Equal("""["a","b"]""", merged.Single(p => p.Key == "Tag").Value);
    }

    [Fact]
    public void MergeParameters_DuplicateRequestKeys_AreReported()
    {
        var errors = new List<string>();
        ReportService.MergeParameters(
            [], [new("Tag", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("a")), new("tag", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("b"))], errors: errors);

        Assert.Contains(errors, e => e.Contains("supplied 2 times", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SerializeParametersJson_DuplicateKeys_Throws()
        => Assert.Throws<ReportValidationException>(() => ReportService.SerializeParametersJson(
            [new("Tag", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("a")), new("TAG", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("b"))]));

    [Fact]
    public void ResolveConfinedPreRenderedPath_InsideTemp_IsAllowed()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lyo-pre-{Guid.NewGuid():N}.pdf");
        var resolved = ReportService.ResolveConfinedPreRenderedPath(path, new() { ConnectionString = "x" });
        Assert.Equal(Path.GetFullPath(path), resolved);
    }

    [Theory]
    [InlineData("/etc/passwd")]
    [InlineData("../../etc/passwd")]
    public void ResolveConfinedPreRenderedPath_OutsideRoots_IsRejected(string path)
        => Assert.Throws<ReportValidationException>(() => ReportService.ResolveConfinedPreRenderedPath(path, new() { ConnectionString = "x" }));

    [Fact]
    public void ResolveConfinedPreRenderedPath_ConfiguredRoot_IsHonored()
    {
        var root = Path.Combine(Path.GetTempPath(), $"lyo-root-{Guid.NewGuid():N}");
        var options = new PostgresReportingOptions { ConnectionString = "x", PreRenderedFileRoots = [root] };
        Assert.Throws<ReportValidationException>(() => ReportService.ResolveConfinedPreRenderedPath(Path.Combine(Path.GetTempPath(), "elsewhere.pdf"), options));
        var inside = Path.Combine(root, "nested", "out.pdf");
        Assert.Equal(Path.GetFullPath(inside), ReportService.ResolveConfinedPreRenderedPath(inside, options));
    }

    private sealed class FakeProvider(string profileKey) : IReportDataProvider
    {
        public string ProfileKey => profileKey;

        public Task<ReportDataProviderResult> BuildAsync(ReportDataProviderRequest request, CancellationToken ct = default) => Task.FromResult(new ReportDataProviderResult());
    }
}