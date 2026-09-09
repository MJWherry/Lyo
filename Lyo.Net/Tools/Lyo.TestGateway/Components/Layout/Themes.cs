using Lyo.Web.Host;
using MudBlazor;

namespace Lyo.TestGateway.Components.Layout;

/// <summary>Delegates to <see cref="LyoThemes.Default" /> so existing pages still compile after the palette moved into the host package.</summary>
public static class Themes
{
    public static MudTheme Default => LyoThemes.Default;
}
