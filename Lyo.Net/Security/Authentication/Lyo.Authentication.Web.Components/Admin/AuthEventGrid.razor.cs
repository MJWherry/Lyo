using Lyo.Api.Client;
using Lyo.Authentication.Models;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;

namespace Lyo.Authentication.Web.Components;

public partial class AuthEventGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("Kind"), new("Outcome"), new("Provider"), new("Reason")];

    private LyoDataGridProjected? _dataGrid;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public string BaseRoute { get; set; } = Constants.Rest.Auth.Route;

    [Parameter]
    public Guid? UserId { get; set; }

    private string _route => $"{BaseRoute.TrimEnd('/')}/Event";

    private void ApplyQuery(ProjectionQueryReqBuilder q)
    {
        if (UserId is { } userId)
            q.AddWhere(new ConditionClause("UserId", ComparisonOperatorEnum.Equals, userId));
    }
}