using Lyo.Algorithms.Models;

namespace Lyo.Algorithms.Web.Components;

/// <summary>Starter drafts and bags. Fishing matches the seed formula: bass points 10, weight 6, and length 15 score 38.</summary>
public static class AlgorithmPresets
{
    /// <summary>Seed fishing expression.</summary>
    public const string FishingFormula = """
                                         fish_type + (weight >= 5 && weight < 10 ? 3 : (weight >= 0 && weight < 5 ? 1 : 0)) + (length < 10 ? 0 : (length > 20 ? 50 : (length - 10) / 10 * 50))
                                         """;

    /// <summary>Untyped fishing formula.</summary>
    public static AlgorithmDraft FishingDraft() => new() { Key = "leaderboard.seed_fishing_0", Kind = AlgorithmKind.Expression, Source = FishingFormula };

    /// <summary>Bag that scores 38 against <see cref="FishingFormula" />.</summary>
    public static List<AlgorithmContextRow> FishingContext()
        => [new() { Name = "fish_type", Alias = "double", Text = "10" }, new() { Name = "weight", Alias = "double", Text = "6" }, new() { Name = "length", Alias = "double", Text = "15" }];

    /// <summary>Untyped attack formula.</summary>
    public static AlgorithmDraft AttackDraft() => new() { Key = "attack.physical", Kind = AlgorithmKind.Expression, Source = "Atk * 2 - Def" };

    /// <summary>Bag for <see cref="AttackDraft" />. Atk 3 and Def 1 score 5.</summary>
    public static List<AlgorithmContextRow> AttackContext() => [new() { Name = "Atk", Alias = "double", Text = "3" }, new() { Name = "Def", Alias = "double", Text = "1" }];

    /// <summary>Decimal, int, and datetime sample from the type tests. The bag scores 11.</summary>
    public static AlgorithmDraft ScheduleDraft()
        => new() {
            Key = "schedule.mix",
            Kind = AlgorithmKind.Expression,
            Result = "decimal",
            Source = "Rate * Convert.ToDecimal(Periods) + (Maturity > Issued && Dates[0] == Issued ? 1m : 0m)",
            Parameters = [
                new() { Name = "Rate", Alias = "decimal" }, new() { Name = "Periods", Alias = "int" }, new() { Name = "Issued", Alias = "datetime" },
                new() { Name = "Maturity", Alias = "datetime" }, new() { Name = "Dates", Alias = "datetime[]" }
            ]
        };

    /// <summary>Bag for <see cref="ScheduleDraft" />.</summary>
    public static List<AlgorithmContextRow> ScheduleContext()
        => [
            new() { Name = "Rate", Alias = "decimal", Text = "2.5" }, new() { Name = "Periods", Alias = "int", Text = "4" },
            new() { Name = "Issued", Alias = "datetime", Text = "2020-01-01T00:00:00" }, new() { Name = "Maturity", Alias = "datetime", Text = "2020-06-01T00:00:00" },
            new() { Name = "Dates", Alias = "datetime[]", Text = """["2020-01-01T00:00:00"]""" }
        ];
}
