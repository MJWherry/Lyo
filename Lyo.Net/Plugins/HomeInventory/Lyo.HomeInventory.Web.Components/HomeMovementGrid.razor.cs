using Lyo.Web.Components.Models;

namespace Lyo.HomeInventory.Web.Components;

/// <summary>Read-only movement history grid.</summary>
public partial class HomeMovementGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("ItemId"), new("MovementType"), new("ReferenceNumber")];

    /// <summary>HTTP client.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>HomeInventory route prefix.</summary>
    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "HomeInventory";

    private string _route => $"{BaseRoute.TrimEnd('/')}/Movements";
}