using System.Text.Json;
using Lyo.Exceptions.Models;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Providers;
using Lyo.Reporting.Postgres;
using Lyo.Reporting.Postgres.Database;
using Microsoft.Extensions.Options;

namespace Lyo.Reporting.Tests;

public sealed class ReportServiceUnitTests
{
    [Fact]
    public void Duplicate_provider_keys_fail_with_actionable_message()
    {
        var ex = Assert.Throws<ConflictException>(() => new ReportService(
            null!, [], [new FakeProvider("dup-key"), new FakeProvider("DUP-KEY")], [], null!, Options.Create(new PostgresReportingOptions { ConnectionString = "x" }), null!));

        Assert.Contains("dup-key", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("unique", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Duplicate_profile_keys_fail_with_actionable_message()
    {
        var ex = Assert.Throws<ConflictException>(() => new ReportService(
            null!, [], [], [new() { Key = "profile-a" }, new() { Key = "Profile-A" }], null!, Options.Create(new PostgresReportingOptions { ConnectionString = "x" }), null!));

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
    public void SanitizeFileName_strips_paths_and_invalid_input(string? input, string? expected) => Assert.Equal(expected, ReportService.SanitizeFileName(input));

    [Fact]
    public void SanitizeFileName_removes_invalid_and_control_chars()
    {
        var sanitized = ReportService.SanitizeFileName("re\0po\trt.csv");
        Assert.Equal("report.csv", sanitized);
    }

    [Fact]
    public void SanitizeFileName_caps_length_preserving_extension()
    {
        var sanitized = ReportService.SanitizeFileName(new string('a', 400) + ".csv");
        Assert.NotNull(sanitized);
        Assert.True(sanitized!.Length <= ReportService.MaxFileNameLength);
        Assert.EndsWith(".csv", sanitized, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeParametersJson_preserves_multi_values_as_arrays()
    {
        var json = ReportService.SerializeParametersJson(
            [new("Tag", ReportParameterType.String, "a"), new("Tag", ReportParameterType.String, "b"), new("Single", ReportParameterType.String, "only")]);

        using var doc = JsonDocument.Parse(json);
        var tag = doc.RootElement.GetProperty("Tag");
        Assert.Equal(JsonValueKind.Array, tag.ValueKind);
        Assert.Equal(["a", "b"], tag.EnumerateArray().Select(e => e.GetString()!).ToArray());
        Assert.Equal("only", doc.RootElement.GetProperty("Single").GetString());
    }

    [Fact]
    public void MergeParameters_keeps_all_values_for_multi_value_keys()
    {
        var def = new List<ReportDefinitionParameter> { new() { Key = "Tag", Type = nameof(ReportParameterType.String), AllowMultiple = true } };
        var merged = ReportService.MergeParameters(def, [new("Tag", ReportParameterType.String, "a"), new("Tag", ReportParameterType.String, "b")]);
        Assert.Equal(2, merged.Count(p => p.Key == "Tag"));
    }

    private sealed class FakeProvider(string profileKey) : IReportDataProvider
    {
        public string ProfileKey => profileKey;

        public Task<ReportDataProviderResult> BuildAsync(ReportDataProviderRequest request, CancellationToken ct = default) => Task.FromResult(new ReportDataProviderResult());
    }
}