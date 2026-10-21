using Lyo.Privacy.Policy;
using Microsoft.AspNetCore.Components;

namespace Lyo.Privacy.Web.Components;

public partial class PolicyFingerprintChip
{
    [Parameter]
    public RedactionPolicy? Policy { get; set; }
}