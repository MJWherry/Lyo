using Lyo.Query.Models.Parameters;

namespace Lyo.Query.Tests;

public sealed class ParameterListJsonTests
{
    [Fact]
    [Trait("Category", "Fast")]
    public void Serialize_Parse_RoundTripsStringArray()
    {
        var json = ParameterListJson.Serialize(["A", "B", "C"]);
        Assert.Equal("""["A","B","C"]""", json);
        Assert.Equal(["A", "B", "C"], ParameterListJson.Parse(json));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void Serialize_Parse_RoundTripsNumberAndBool()
    {
        var numbers = ParameterListJson.Serialize(["1", "2.5", "x"], ParameterListJsonKind.Number);
        Assert.Equal("[1,2.5]", numbers);
        Assert.Equal(["1", "2.5"], ParameterListJson.Parse(numbers));

        var flags = ParameterListJson.Serialize(["true", "False", "nope"], ParameterListJsonKind.Bool);
        Assert.Equal("[true,false]", flags);
        Assert.Equal([bool.TrueString, bool.FalseString], ParameterListJson.Parse(flags));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void Parse_EmptyOrInvalid_ReturnsEmpty_NoPipeLegacy()
    {
        Assert.Empty(ParameterListJson.Parse(null));
        Assert.Empty(ParameterListJson.Parse(""));
        Assert.Empty(ParameterListJson.Parse("A|B|C"));
        Assert.Empty(ParameterListJson.Parse("{}"));
        Assert.Empty(ParameterListJson.Parse("not-json"));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void Serialize_Empty_ReturnsNull()
    {
        Assert.Null(ParameterListJson.Serialize(null));
        Assert.Null(ParameterListJson.Serialize([]));
        Assert.Null(ParameterListJson.Serialize(["", "  "]));
    }
}
