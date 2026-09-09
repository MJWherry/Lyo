using System.Text.Json;
using Lyo.Web.Components.DataGrid;

namespace Lyo.Web.Components.Tests;

public class ProjectedValueHelperBoolTests
{
    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    [InlineData("TRUE")]
    [InlineData("t")]
    [InlineData("1")]
    [InlineData("y")]
    [InlineData("yes")]
    [InlineData("on")]
    public void ToBool_LenientTrueTokens_ReturnTrue(string value) => Assert.True(ProjectedValueHelper.ToBool(value));

    [Theory]
    [InlineData("false")]
    [InlineData("False")]
    [InlineData("f")]
    [InlineData("0")]
    [InlineData("n")]
    [InlineData("no")]
    [InlineData("off")]
    [InlineData("maybe")]
    [InlineData("")]
    public void ToBool_FalseAndUnknownTokens_ReturnFalse(string value) => Assert.False(ProjectedValueHelper.ToBool(value));

    [Fact]
    public void ToBool_Null_ReturnsFalse() => Assert.False(ProjectedValueHelper.ToBool(null));

    [Fact]
    public void ToBool_ActualBooleans_PassThrough()
    {
        Assert.True(ProjectedValueHelper.ToBool(true));
        Assert.False(ProjectedValueHelper.ToBool(false));
    }

    [Theory]
    [InlineData("""{"Enabled":true}""", true)]
    [InlineData("""{"Enabled":false}""", false)]
    [InlineData("""{"Enabled":"yes"}""", true)]
    [InlineData("""{"Enabled":"off"}""", false)]
    [InlineData("""{"Enabled":"1"}""", true)]
    [InlineData("""{"Other":true}""", false)]
    public void GetBool_ReadsProjectedJsonField(string json, bool expected)
        => Assert.Equal(expected, ProjectedValueHelper.GetBool(JsonDocument.Parse(json).RootElement, "Enabled"));

    [Fact]
    public void GetBool_ReadsDictionaryField()
    {
        var row = new Dictionary<string, object?> { ["IsActive"] = "yes" };
        Assert.True(ProjectedValueHelper.GetBool(row, "IsActive"));
    }
}
