using Lyo.Web.Components.Models;

namespace Lyo.TestGateway.Components.Pages.People;

public partial class PersonEmailAddressGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("Email"), new("Label")];
}