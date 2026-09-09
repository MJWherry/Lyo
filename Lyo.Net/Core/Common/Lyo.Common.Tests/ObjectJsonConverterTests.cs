using System.Globalization;
using System.Text.Json;
using Lyo.Common.Json.JsonConverters;

namespace Lyo.Common.Tests;

public sealed class ObjectJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new ObjectJsonConverter() } };

    [Fact]
    public void Deserialize_String_ReturnsStringNotJsonElement()
    {
        var back = JsonSerializer.Deserialize<object>("\"Ada\"", Options);
        Assert.Equal("Ada", back);
        Assert.IsType<string>(back);
    }

    [Fact]
    public void Deserialize_List_ReturnsClrPrimitives()
    {
        var back = JsonSerializer.Deserialize<List<object?>>("""["Ada",9,true,null]""", Options);
        Assert.NotNull(back);
        Assert.Equal("Ada", back[0]);
        Assert.Equal(9, Convert.ToInt32(back[1], CultureInfo.InvariantCulture));
        Assert.Equal(true, back[2]);
        Assert.Null(back[3]);
    }

    [Fact]
    public void RoundTrip_StringObject_StaysString()
    {
        var json = JsonSerializer.Serialize((object)"Ada", Options);
        var back = JsonSerializer.Deserialize<object>(json, Options);
        Assert.Equal("Ada", back);
        Assert.IsType<string>(back);
    }
}
