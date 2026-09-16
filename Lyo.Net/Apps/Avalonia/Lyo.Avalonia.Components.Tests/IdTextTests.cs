namespace Lyo.Desktop.Components.Tests;

public class IdTextTests
{
    [Fact]
    public void Abbreviate_KeepsTheLastNineCharacters()
    {
        Assert.Equal("369e37990", IdText.Abbreviate("01a11154-18f4-73bf-86cb-199369e37990"));
    }

    [Fact]
    public void Abbreviate_LeavesShortAndNumericValuesWhole()
    {
        Assert.Equal("folder", IdText.Abbreviate("folder"));
        Assert.Equal("12345678901", IdText.Abbreviate("12345678901"));
        Assert.Equal("", IdText.Abbreviate("  "));
    }
}
