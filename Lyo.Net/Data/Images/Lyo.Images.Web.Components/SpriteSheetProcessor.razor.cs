using Microsoft.AspNetCore.Components;

namespace Lyo.Images.Web.Components;

public partial class SpriteSheetProcessor
{
    [Parameter]
    public bool HasSpriteSheet { get; set; }

    [Parameter]
    public bool Busy { get; set; }

    [Parameter]
    public int RowCount { get; set; }

    [Parameter]
    public EventCallback<int> RowCountChanged { get; set; }

    [Parameter]
    public int FramesPerRow { get; set; }

    [Parameter]
    public EventCallback<int> FramesPerRowChanged { get; set; }

    [Parameter]
    public int OffsetX { get; set; }

    [Parameter]
    public EventCallback<int> OffsetXChanged { get; set; }

    [Parameter]
    public int OffsetY { get; set; }

    [Parameter]
    public EventCallback<int> OffsetYChanged { get; set; }

    [Parameter]
    public int LeftTrim { get; set; }

    [Parameter]
    public EventCallback<int> LeftTrimChanged { get; set; }

    [Parameter]
    public int RightTrim { get; set; }

    [Parameter]
    public EventCallback<int> RightTrimChanged { get; set; }

    [Parameter]
    public int TopTrim { get; set; }

    [Parameter]
    public EventCallback<int> TopTrimChanged { get; set; }

    [Parameter]
    public int BottomTrim { get; set; }

    [Parameter]
    public EventCallback<int> BottomTrimChanged { get; set; }

    [Parameter]
    public EventCallback OnApply { get; set; }
}
