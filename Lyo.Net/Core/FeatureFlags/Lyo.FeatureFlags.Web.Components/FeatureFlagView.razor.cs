using Lyo.FeatureFlags;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FeatureFlags.Web.Components;

/// <summary>
/// Renders <see cref="ChildContent" /> when <see cref="IFeatureFlagService.IsEnabled" /> is true for <see cref="Name" />.
/// A missing service or a disabled flag renders nothing. Pass <see cref="Context" /> when the flag uses rules or a partial percentage.
/// </summary>
public partial class FeatureFlagView
{
    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    /// <summary>Flag name passed to <see cref="IFeatureFlagService.IsEnabled" />.</summary>
    [Parameter]
    [EditorRequired]
    public string Name { get; set; } = "";

    /// <summary>Caller for rules and partial percentages. Null is an anonymous caller.</summary>
    [Parameter]
    public FeatureFlagContext? Context { get; set; }

    /// <summary>Content shown when the flag is on.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool Enabled
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Name))
                return false;

            return Services.GetService<IFeatureFlagService>()?.IsEnabled(Name, Context) == true;
        }
    }
}
