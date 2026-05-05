using System.Linq;

namespace Lyo.ContentThreatScan.Tests;

public sealed class DefaultContentThreatScannerTests
{
    [Fact]
    public async Task Harmless_json_produces_clean_contribution_list_when_eligible()
    {
        var opts = new ContentThreatHeuristicOptions();
        DefaultContentThreatScanner sut = new(opts);
        var ctx = new ContentThreatScanContext("data.json", "application/json");

        byte[] harmless = "{\"hello\":\"world\"}"u8.ToArray();
        IReadOnlyList<ContentThreatContribution> hits =
            await sut.CollectHeuristicContributionsAsync(harmless, ctx, TestContext.Current.CancellationToken);

        Assert.Empty(hits);
    }

    [Fact]
    public async Task Union_select_boosts_scores_for_json_eligible_streams()
    {
        var opts = new ContentThreatHeuristicOptions();

        DefaultContentThreatScanner sut = new(opts);
        byte[] hostile = "{\"payload\":\" UNION SELECT username FROM accounts\"}"u8.ToArray();
        var hits = await sut.CollectHeuristicContributionsAsync(
            hostile,
            new ContentThreatScanContext("evil.json", "application/json"),
            TestContext.Current.CancellationToken);

        Assert.Contains(
            hits,
            contribution =>
                string.Equals(contribution.RuleId, "sql.union_select", StringComparison.Ordinal));
        Assert.True(hits.Sum(point => point.Points) >= 35m);
    }

    [Fact]
    public async Task Binary_prefix_short_circuits_when_nul_presence_detected()
    {
        ContentThreatHeuristicOptions opts =
            new() { SkipIfLikelyBinary = true, TreatNullOctetAsBinary = true };

        DefaultContentThreatScanner sut = new(opts);
        byte[] buffer = [(byte)'{', (byte)'a', (byte)',', (byte)',', (byte)',', (byte)',', (byte)',', (byte)',', (byte)',', (byte)',', (byte)0, (byte)'}'];

        IReadOnlyList<ContentThreatContribution> hits =
            await sut.CollectHeuristicContributionsAsync(
                buffer,
                new ContentThreatScanContext("sample.json", contentType: "application/json"),
                TestContext.Current.CancellationToken);

        Assert.Empty(hits);
    }
}
