using System.Text.Json;
using Lyo.Algorithms;
using Lyo.Algorithms.Models;
using Lyo.Algorithms.Models.Request;
using Lyo.Exceptions.Models;
using Lyo.Leaderboard;
using Lyo.Leaderboard.Enums;

namespace Lyo.Leaderboard.Postgres;

/// <summary>Reads and writes one expression algorithm per board. Property rows stay form inputs.</summary>
internal sealed class LeaderboardCatalog
{
    private readonly IAlgorithmCatalog _catalog;
    private readonly IAlgorithmRegistry _registry;

    public LeaderboardCatalog(IAlgorithmCatalog catalog, IAlgorithmRegistry registry)
    {
        _catalog = catalog;
        _registry = registry;
    }

    public static string KeyFor(string slug)
    {
        var folded = slug.Trim().ToLowerInvariant().Replace('-', '_');
        return "leaderboard." + folded;
    }

    /// <summary>Copies the latest expression source onto <paramref name="board" />. A missing revision leaves the formula empty.</summary>
    public async Task ApplyFormulaAsync(BoardRecord board, CancellationToken ct)
    {
        board.ScoreFormula = await LoadSourceAsync(board.AlgorithmKey, ct).ConfigureAwait(false);
    }

    /// <summary>Publishes <paramref name="formula" /> as the latest expression revision. An empty formula leaves the catalog unchanged.</summary>
    public async Task SaveFormulaAsync(string algorithmKey, string? formula, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(formula))
            return;

        var result = await _catalog.SaveRevisionAsync(
            algorithmKey, new AlgorithmRevisionReq { Kind = AlgorithmKind.Expression, Source = formula, Name = algorithmKey }, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
            throw new BadRequestException(result.Errors![0].Message);
    }

    /// <summary>
    /// Scores one entry. Choice columns write their number under the property key and the choice key under <c>{key}_key</c>.
    /// A missing number is 0. No published formula scores 0.
    /// </summary>
    public async Task<(decimal Total, string? Breakdown)> ScoreAsync(
        string algorithmKey,
        IReadOnlyList<ScoringPropertyRecord> properties,
        IReadOnlyList<EntryValueRecord> values,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(await LoadSourceAsync(algorithmKey, ct).ConfigureAwait(false)))
            return (0m, null);

        var bag = new AlgorithmContextBag();
        foreach (var value in values) {
            var property = Find(properties, value);
            if (property == null || !AlgorithmContextBag.IsIdentifier(property.Key))
                continue;

            if (property.ValueKind == ScoringValueKind.Choice) {
                if (string.IsNullOrEmpty(value.ChoiceKey))
                    continue;

                var choice = property.Choices.FirstOrDefault(c => string.Equals(c.Key, value.ChoiceKey, StringComparison.OrdinalIgnoreCase));
                if (choice == null)
                    throw new BadRequestException($"Choice '{value.ChoiceKey}' is not defined on '{property.Key}'.");

                bag.Set(property.Key, choice.Value);
                var keyName = property.Key + "_key";
                if (AlgorithmContextBag.IsIdentifier(keyName))
                    bag.Set(keyName, choice.Key);
                continue;
            }

            if (value.NumberValue is { } number)
                bag.Set(property.Key, (double)number);
            else if (value.BoolValue is { } flag)
                bag.Set(property.Key, flag ? 1d : 0d);
        }

        foreach (var property in properties) {
            if (!AlgorithmContextBag.IsIdentifier(property.Key) || bag.ContainsKey(property.Key))
                continue;

            if (property.ValueKind is ScoringValueKind.Number or ScoringValueKind.Integer or ScoringValueKind.Duration or ScoringValueKind.Boolean)
                bag.Set(property.Key, 0d);
        }

        try {
            var execution = await _registry.ExecuteWithBreakdownAsync<AlgorithmContextBag, double>(AlgorithmRef.Latest(algorithmKey), bag, ct).ConfigureAwait(false);
            var steps = execution.Steps;
            return ((decimal)execution.Value, steps.Count == 0 ? null : JsonSerializer.Serialize(steps, LeaderboardAlgorithmJson.Options));
        }
        catch (AlgorithmException ex) {
            throw new BadRequestException(ex.Message);
        }
    }

    private async Task<string?> LoadSourceAsync(string algorithmKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(algorithmKey))
            return null;

        try {
            var revisions = await _catalog.GetRevisionsAsync(algorithmKey, ct).ConfigureAwait(false);
            var source = revisions.OrderByDescending(r => r.Revision).FirstOrDefault()?.Source;
            return string.IsNullOrWhiteSpace(source) ? null : source;
        }
        catch (AlgorithmException) {
            return null;
        }
    }

    private static ScoringPropertyRecord? Find(IReadOnlyList<ScoringPropertyRecord> properties, EntryValueRecord value)
        => properties.FirstOrDefault(p => value.PropertyId != Guid.Empty && p.Id == value.PropertyId)
            ?? properties.FirstOrDefault(p => !string.IsNullOrWhiteSpace(value.PropertyKey) && string.Equals(p.Key, value.PropertyKey, StringComparison.OrdinalIgnoreCase));
}
