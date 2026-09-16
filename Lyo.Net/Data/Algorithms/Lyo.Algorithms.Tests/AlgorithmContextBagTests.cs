namespace Lyo.Algorithms.Tests;

public class AlgorithmContextBagTests
{
    private readonly ExpressionCompiler _compiler = new();

    [Fact]
    public void Set_DottedKey_Throws()
    {
        var bag = new AlgorithmContextBag();

        Assert.Throws<ArgumentException>(() => bag.Set("Docket.Number", 1));
        Assert.Throws<ArgumentException>(() => bag.Set("Docket.Number", () => 1));
    }

    [Fact]
    public void Execute_ValueFactory_MaterializesOnce()
    {
        var bag = new AlgorithmContextBag();
        var calls = 0;
        bag.Set("Atk", () => {
            calls++;
            return 10d;
        });
        var compiled = _compiler.Compile<AlgorithmContextBag, double>("Atk * 2");

        Assert.Equal(20d, compiled.Data!.Execute(bag));
        Assert.Equal(20d, compiled.Data.Execute(bag));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Execute_BagFactory_ReadsSibling()
    {
        var bag = new AlgorithmContextBag();
        bag.Set("Def", 3d);
        bag.Set("Atk", current => current.GetRequired<double>("Def") * 2);
        var compiled = _compiler.Compile<AlgorithmContextBag, double>("Atk + 1");

        Assert.Equal(7d, compiled.Data!.Execute(bag));
    }

    [Fact]
    public void TryGetAs_StringNumber_ReturnsDouble()
    {
        var bag = new AlgorithmContextBag();
        bag.Set("Atk", "15");

        Assert.True(bag.TryGetAs<double>("Atk", out var value));
        Assert.Equal(15d, value);
    }
}
