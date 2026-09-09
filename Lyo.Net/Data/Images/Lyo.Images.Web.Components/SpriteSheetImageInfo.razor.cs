using System.Net.Http.Json;
using Lyo.Images.Models;
using Lyo.Images.Sprite;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Images.Web.Components;

public partial class SpriteSheetImageInfo
{
    [Parameter]
    public ImageMetadata? Metadata { get; set; }

    [Parameter]
    public SpriteSheetCalculation Calculation { get; set; } = null!;
}
