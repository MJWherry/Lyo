using System.Text.Json;

namespace Lyo.Api.Tests;

public sealed class ExtensionsConvertToTypeTests
{
    private static JsonElement JsonArray(params string[] values) => JsonSerializer.SerializeToElement(values);

    [Fact]
    public void ConvertToType_JsonArray_ToStringList_ReturnsList()
    {
        var result = JsonArray("a", "b").ConvertToType(typeof(List<string>));
        var list = Assert.IsType<List<string>>(result);
        Assert.Equal(["a", "b"], list);
    }

    [Fact]
    public void ConvertToType_JsonArray_ToStringArray_ReturnsArray()
    {
        var result = JsonArray("a", "b").ConvertToType(typeof(string[]));
        var array = Assert.IsType<string[]>(result);
        Assert.Equal(["a", "b"], array);
    }

    [Fact]
    public void ConvertToType_JsonArray_ToIReadOnlyList_ReturnsAssignable()
    {
        var result = JsonArray("a", "b").ConvertToType(typeof(IReadOnlyList<string>));
        var list = Assert.IsAssignableFrom<IReadOnlyList<string>>(result);
        Assert.Equal(["a", "b"], list);
    }

    [Fact]
    public void ConvertToType_JsonArray_ToHashSet_ReturnsHashSet()
    {
        var result = JsonArray("a", "b", "a").ConvertToType(typeof(HashSet<string>));
        var set = Assert.IsType<HashSet<string>>(result);
        Assert.Equal(2, set.Count);
    }

    [Fact]
    public void ConvertToType_SingleValue_ToStringList_WrapsInList()
    {
        var result = "a".ConvertToType(typeof(List<string>));
        var list = Assert.IsType<List<string>>(result);
        Assert.Equal(["a"], list);
    }

    [Fact]
    public void ConvertToType_JsonArrayOfNumbers_ToIntList_ReturnsList()
    {
        var result = JsonSerializer.SerializeToElement(new[] { 1, 2, 3 }).ConvertToType(typeof(List<int>));
        var list = Assert.IsType<List<int>>(result);
        Assert.Equal([1, 2, 3], list);
    }
}
