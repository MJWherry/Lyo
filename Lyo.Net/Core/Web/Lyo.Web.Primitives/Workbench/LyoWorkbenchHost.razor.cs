using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

/// <summary>
/// Host page that renders the workbench whose <see cref="ILyoWorkbenchDescriptor.Slug" /> matches the route. Adding a package's UI is then a DI registration, not
/// a new page in each host.
/// </summary>
/// <remarks>
/// Map the primitives assembly on the host with <c>AddAdditionalAssemblies(typeof(LyoWorkbenchHost).Assembly)</c> so this <c>@page</c> is found.
/// </remarks>
public partial class LyoWorkbenchHost
{
    /// <summary>Slug from the URL, matched case-insensitively against the registry.</summary>
    [Parameter]
    public string Slug { get; set; } = string.Empty;

    [Inject]
    private LyoWorkbenchRegistry Registry { get; set; } = null!;

    private ILyoWorkbenchDescriptor? Descriptor => Registry.Find(Slug);
}
