using System.Net;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.Catalog;

public partial class CatalogSectionList
{
    /// <summary>Catalog sections to draw.</summary>
    [Parameter]
    public IReadOnlyList<CatalogSection>? Sections { get; set; }

    private static string FormatInline(string text) => WebUtility.HtmlEncode(text).Replace("\n", "<br/>", StringComparison.Ordinal);
}
