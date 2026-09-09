using Microsoft.AspNetCore.Components;

namespace Lyo.MessageQueue.Web.Components;

public partial class MessageQueueHealthPanel
{
    [Parameter]
    public bool Busy { get; set; }

    [Parameter]
    public EventCallback OnCheckHealth { get; set; }
}
