using Lyo.Web.Components.Models;

namespace Lyo.TestGateway.Components.Pages.People;

public partial class PersonPhoneNumberGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("Number"), new("Label"), new("TechnologyType")];
}