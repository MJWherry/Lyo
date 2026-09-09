using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

public partial class LyoWorkbenchStatus
{
    /// <summary>Status text. Nothing renders while this is null or whitespace, so the banner does not reserve layout space while idle.</summary>
    [Parameter]
    public string? Message { get; set; }

    /// <summary>Severity that drives the alert colour.</summary>
    [Parameter]
    public Severity Severity { get; set; } = Severity.Info;

    /// <summary>Renders the alert in its dense form. On by default because these banners sit inside tight panels.</summary>
    [Parameter]
    public bool Dense { get; set; } = true;

    /// <summary>Extra CSS classes, usually margin utilities such as <c>my-1</c>.</summary>
    [Parameter]
    public string? Class { get; set; }
}
