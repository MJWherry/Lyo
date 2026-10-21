using Lyo.TestGateway.Models;
using Microsoft.AspNetCore.Components;

namespace Lyo.TestGateway.Components.Pages.People;

public partial class EndatoPsPersonView
{
    [Parameter]
    [EditorRequired]
    public EndatoPsPersonRes Person { get; set; } = null!;

    private IReadOnlyList<string> PhoneTypes => PersonViewHelpers.PhoneTypeOptions(null, Person.PhoneNumbers.Select(p => p.Type));
}