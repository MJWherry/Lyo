using System.Text.Json;

namespace Lyo.Desktop.Components.Tests;

public class EdgeDockPlacementTests
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void Apply_RoundTripsTheSavedFields()
    {
        var placement = new EdgeDockPlacement();
        placement.Apply(true, 0, 4, 8, 10, 20);
        Assert.Equal((true, 0, 4, 8, 10, 20), placement.Values());
        Assert.Equal(8, placement.Along);
        placement.Edge = ScreenEdge.Top;
        Assert.Equal(4, placement.Along);
    }

    [Fact]
    public void Apply_EdgeOutsideTheEnum_Clamps()
    {
        var placement = new EdgeDockPlacement();
        placement.Apply(false, 9, 1, 2, 3, 4);
        Assert.Equal(ScreenEdge.Bottom, placement.Edge);
        placement.Apply(false, -1, 1, 2, 3, 4);
        Assert.Equal(ScreenEdge.Left, placement.Edge);
    }

    [Fact]
    public void Json_RoundTripsEdgeAsANumber()
    {
        var placement = new EdgeDockPlacement {
            Docked = true,
            Edge = ScreenEdge.Left,
            X = 4,
            Y = 8,
            RestX = 10,
            RestY = 20
        };

        var json = JsonSerializer.Serialize(placement, Json);
        Assert.Contains("\"edge\":0", json.Replace(" ", ""));
        var again = JsonSerializer.Deserialize<EdgeDockPlacement>(json, Json);
        Assert.NotNull(again);
        Assert.Equal(placement.Values(), again.Values());
    }

    [Fact]
    public void Pixels_FixedSize_ScalesBothAxes()
    {
        var rest = new EdgeDockRest(1100, 720, false);
        Assert.Equal((2200, 1440), rest.Pixels(2));
    }

    [Fact]
    public void Pixels_HeightToContent_SkipsHeight()
    {
        var rest = new EdgeDockRest(360, 720, true);
        Assert.Equal((720, 0), rest.Pixels(2));
    }

    [Fact]
    public void Pixels_NonPositiveScale_UsesOne()
    {
        var rest = new EdgeDockRest(360, 200, false);
        Assert.Equal((360, 200), rest.Pixels(0));
        Assert.Equal((360, 200), rest.Pixels(-1));
    }
}