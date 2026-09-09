using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;

namespace Lyo.TestGateway.Components.Pages.People;

public partial class EndatoPsAddressGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("FullAddress"), new("City"), new("State"), new("Zipcode")];
}
