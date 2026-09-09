using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;

namespace Lyo.TestGateway.Components.Pages.People;

public partial class EndatoCeAddressGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("Street"), new("City"), new("State"), new("Zipcode")];
}
