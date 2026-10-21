using Lyo.Web.Components.Models;

namespace Lyo.TestGateway.Components.Pages.People;

public partial class PersonAddressGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("FullAddress"), new("City"), new("State"), new("Zipcode")];
}