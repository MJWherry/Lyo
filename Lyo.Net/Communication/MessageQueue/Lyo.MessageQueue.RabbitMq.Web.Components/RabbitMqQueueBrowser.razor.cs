using System.Net.Http.Json;
using Lyo.Common.Metadata.Records;
using Lyo.MessageQueue;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Lyo.Web.Components.DataGrid;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.MessageQueue.RabbitMq.Web.Components;

public partial class RabbitMqQueueBrowser
{
    /// <summary>Current queue snapshots from the management API.</summary>
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<MessageQueueInfo> Queues { get; set; } = [];

    /// <summary>Named exchanges shown in the bind dropdown.</summary>
    [Parameter]
    public IReadOnlyList<string> ExchangeNames { get; set; } = [];

    /// <summary>Name of the selected queue. Empty when nothing is selected.</summary>
    [Parameter]
    public string SelectedName { get; set; } = "";

    /// <summary>Fired when the selected queue changes.</summary>
    [Parameter]
    public EventCallback<string> SelectedNameChanged { get; set; }

    /// <summary>How many messages to peek.</summary>
    [Parameter]
    public int PeekCount { get; set; } = 10;

    /// <summary>Fired when peek count changes.</summary>
    [Parameter]
    public EventCallback<int> PeekCountChanged { get; set; }

    /// <summary>Auto-refresh interval, in seconds.</summary>
    [Parameter]
    public int RefreshIntervalSeconds { get; set; } = 5;

    /// <summary>Fired when the refresh interval changes.</summary>
    [Parameter]
    public EventCallback<int> RefreshIntervalSecondsChanged { get; set; }

    /// <summary>If true, the workbench polls queue stats.</summary>
    [Parameter]
    public bool AutoRefresh { get; set; }

    /// <summary>Fired when auto-refresh is toggled.</summary>
    [Parameter]
    public EventCallback<bool> AutoRefreshChanged { get; set; }

    /// <summary>Disables toolbar and row actions while a call is running.</summary>
    [Parameter]
    public bool Busy { get; set; }

    /// <summary>Exchange name on the bind form.</summary>
    [Parameter]
    public string BindExchangeName { get; set; } = "";

    /// <summary>Fired when the bind exchange name changes.</summary>
    [Parameter]
    public EventCallback<string> BindExchangeNameChanged { get; set; }

    /// <summary>Routing key on the bind form.</summary>
    [Parameter]
    public string RoutingKey { get; set; } = "";

    /// <summary>Fired when the routing key changes.</summary>
    [Parameter]
    public EventCallback<string> RoutingKeyChanged { get; set; }

    /// <summary>Publish editor shown on the Send tab.</summary>
    [Parameter]
    public RenderFragment? DetailContent { get; set; }

    /// <summary>Reload the queue list.</summary>
    [Parameter]
    public EventCallback OnRefresh { get; set; }

    /// <summary>Opens the create-queue dialog.</summary>
    [Parameter]
    public EventCallback OnCreate { get; set; }

    /// <summary>Peeks the selected queue.</summary>
    [Parameter]
    public EventCallback<MessageQueueInfo> OnPeek { get; set; }

    /// <summary>Clears the selected queue (parent confirms).</summary>
    [Parameter]
    public EventCallback<MessageQueueInfo> OnClear { get; set; }

    /// <summary>Deletes the selected queue (parent confirms).</summary>
    [Parameter]
    public EventCallback<MessageQueueInfo> OnDelete { get; set; }

    /// <summary>Binds the selected queue to the form's exchange.</summary>
    [Parameter]
    public EventCallback OnBind { get; set; }

    private MessageQueueInfo? SelectedQueue
        => string.IsNullOrWhiteSpace(SelectedName) ? null : Queues.FirstOrDefault(q => q.Name == SelectedName);

    private Task OnQueueChanged(MessageQueueInfo? queue)
        => SelectedNameChanged.InvokeAsync(queue?.Name ?? "");

    private Task PeekSelectedAsync()
        => SelectedQueue is { } queue ? OnPeek.InvokeAsync(queue) : Task.CompletedTask;

    private Task ClearSelectedAsync()
        => SelectedQueue is { } queue ? OnClear.InvokeAsync(queue) : Task.CompletedTask;

    private Task DeleteSelectedAsync()
        => SelectedQueue is { } queue ? OnDelete.InvokeAsync(queue) : Task.CompletedTask;

    private async Task CopySelectedNameAsync()
    {
        if (SelectedQueue is null)
            return;

        await Js.SendToClipboard(SelectedQueue.Name);
        Snackbar.Add("Copied queue name", Severity.Info);
    }

    private Task<IEnumerable<MessageQueueInfo>> SearchQueues(string? value, CancellationToken ct)
    {
        IEnumerable<MessageQueueInfo> items = Queues;
        if (!string.IsNullOrWhiteSpace(value)) {
            var term = value.Trim();
            items = items.Where(q => q.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(items);
    }

    private Task<IEnumerable<string>> SearchBindExchanges(string? value, CancellationToken ct)
        => Task.FromResult(FilterNames(ExchangeNames, value));

    private static IEnumerable<string> FilterNames(IEnumerable<string> names, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return names;

        var term = value.Trim();
        return names.Where(n => n.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static string QueueName(MessageQueueInfo? queue) => queue?.Name ?? "";
}
