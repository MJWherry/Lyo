using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;

namespace Lyo.TestGateway.Components.Pages.People;

public partial class EndatoCeEmailAddressGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("Email")];
}
