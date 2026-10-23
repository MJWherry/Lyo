using Lyo.Health;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.MessageQueue.Web.Components;

public partial class MessageQueueResultPanel
{
    [Parameter]
    public bool Connected { get; set; }

    [Parameter]
    public HealthResult? LastHealth { get; set; }

    [Parameter]
    public IReadOnlyList<KeyValuePair<string, string>> Details { get; set; } = [];

    [Parameter]
    public IReadOnlyList<QueuePeekMessage> PeekedMessages { get; set; } = [];

    [Parameter]
    public string StatusMessage { get; set; } = string.Empty;

    [Parameter]
    public Severity StatusSeverity { get; set; } = Severity.Info;
}