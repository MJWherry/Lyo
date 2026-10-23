namespace Lyo.Algorithms.Tests;

public sealed class AlgorithmExpressionSuggestTests
{
    [Fact]
    public void Suggestions_PrefixAt_IncludesBagNameAndOmitsAtan()
    {
        const string source = "Atk * 2";
        var chips = AlgorithmExpressionHelp.Identifiers(source);
        Assert.Equal(["Atk"], chips);
        var suggestions = AlgorithmExpressionHelp.Suggestions("At", chips, ["attack.physical"]);
        Assert.Contains("Atk", suggestions);
        Assert.DoesNotContain("Math.Atan", suggestions);
    }
}
