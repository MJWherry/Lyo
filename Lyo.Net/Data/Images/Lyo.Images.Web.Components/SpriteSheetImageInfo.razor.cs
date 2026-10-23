using Lyo.Images.Models;
using Lyo.Images.Sprite;
using Microsoft.AspNetCore.Components;

namespace Lyo.Images.Web.Components;

public partial class SpriteSheetImageInfo
{
    [Parameter]
    public ImageMetadata? Metadata { get; set; }

    [Parameter]
    public SpriteSheetCalculation Calculation { get; set; } = null!;
}