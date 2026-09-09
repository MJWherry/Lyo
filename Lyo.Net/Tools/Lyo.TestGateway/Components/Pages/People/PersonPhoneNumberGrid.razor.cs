using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;

namespace Lyo.TestGateway.Components.Pages.People;

public partial class PersonPhoneNumberGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("Number"), new("Label"), new("TechnologyType")];
}
