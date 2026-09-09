using Microsoft.AspNetCore.Components;

namespace Lyo.Images.Web.Components;

public partial class SpriteSheetPlayer
{
    [Parameter]
    public bool Playing { get; set; }

    [Parameter]
    public bool Busy { get; set; }

    [Parameter]
    public bool HasIncludedFrames { get; set; }

    [Parameter]
    public int FramesPerSecond { get; set; }

    [Parameter]
    public EventCallback<int> FramesPerSecondChanged { get; set; }

    [Parameter]
    public int SelectedPreviewIndex { get; set; }

    [Parameter]
    public EventCallback<int> SelectedPreviewIndexChanged { get; set; }

    [Parameter]
    public int MaxPreviewFrameIndex { get; set; }

    [Parameter]
    public int IncludedFrameCount { get; set; }

    [Parameter]
    public EventCallback OnTogglePlayPause { get; set; }
}
