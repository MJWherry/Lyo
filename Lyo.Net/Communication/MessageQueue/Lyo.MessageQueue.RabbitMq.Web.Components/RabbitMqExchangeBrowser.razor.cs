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

public partial class RabbitMqExchangeBrowser
{
    /// <summary>Current exchange snapshots from the management API.</summary>
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<MessageExchangeInfo> Exchanges { get; set; } = [];

    /// <summary>Queue names shown in the bind dropdown.</summary>
    [Parameter]
    public IReadOnlyList<string> QueueNames { get; set; } = [];

    /// <summary>If true, the unnamed and <c>amq.*</c> exchanges are included.</summary>
    [Parameter]
    public bool ShowDefaults { get; set; }

    /// <summary>Fired when the defaults toggle changes.</summary>
    [Parameter]
    public EventCallback<bool> ShowDefaultsChanged { get; set; }

    /// <summary>Name of the selected exchange. Empty when nothing is selected.</summary>
    [Parameter]
    public string SelectedName { get; set; } = "";

    /// <summary>Fired when the selected exchange changes.</summary>
    [Parameter]
    public EventCallback<string> SelectedNameChanged { get; set; }

    /// <summary>Disables toolbar and row actions while a call is running.</summary>
    [Parameter]
    public bool Busy { get; set; }

    /// <summary>Queue name on the bind form.</summary>
    [Parameter]
    public string BindQueueName { get; set; } = "";

    /// <summary>Fired when the bind queue name changes.</summary>
    [Parameter]
    public EventCallback<string> BindQueueNameChanged { get; set; }

    /// <summary>Routing key for publish and bind.</summary>
    [Parameter]
    public string RoutingKey { get; set; } = "";

    /// <summary>Fired when the routing key changes.</summary>
    [Parameter]
    public EventCallback<string> RoutingKeyChanged { get; set; }

    /// <summary>Publish editor shown on the Send tab.</summary>
    [Parameter]
    public RenderFragment? DetailContent { get; set; }

    /// <summary>Reload the exchange list.</summary>
    [Parameter]
    public EventCallback OnRefresh { get; set; }

    /// <summary>Opens the create-exchange dialog.</summary>
    [Parameter]
    public EventCallback OnCreate { get; set; }

    /// <summary>Deletes the selected exchange (parent confirms). Disabled for broker defaults.</summary>
    [Parameter]
    public EventCallback<MessageExchangeInfo> OnDelete { get; set; }

    /// <summary>Binds the form's queue to the selected exchange.</summary>
    [Parameter]
    public EventCallback OnBind { get; set; }

    private IReadOnlyList<MessageExchangeInfo> VisibleExchanges
        => ShowDefaults ? Exchanges : Exchanges.Where(e => !RabbitMqColorHelper.IsDefaultExchange(e.Name)).ToList();

    private MessageExchangeInfo? SelectedExchange
        => string.IsNullOrWhiteSpace(SelectedName) ? null : VisibleExchanges.FirstOrDefault(e => e.Name == SelectedName);

    private Task OnExchangeChanged(MessageExchangeInfo? exchange)
        => SelectedNameChanged.InvokeAsync(exchange?.Name ?? "");

    private Task DeleteSelectedAsync()
        => SelectedExchange is { } exchange ? OnDelete.InvokeAsync(exchange) : Task.CompletedTask;

    private async Task CopySelectedNameAsync()
    {
        if (SelectedExchange is null || string.IsNullOrEmpty(SelectedExchange.Name))
            return;

        await Js.SendToClipboard(SelectedExchange.Name);
        Snackbar.Add("Copied exchange name", Severity.Info);
    }

    private Task<IEnumerable<MessageExchangeInfo>> SearchExchanges(string? value, CancellationToken ct)
    {
        IEnumerable<MessageExchangeInfo> items = VisibleExchanges.Where(e => !string.IsNullOrEmpty(e.Name));
        if (!string.IsNullOrWhiteSpace(value)) {
            var term = value.Trim();
            items = items.Where(e => e.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(items);
    }

    private Task<IEnumerable<string>> SearchBindQueues(string? value, CancellationToken ct)
        => Task.FromResult(FilterNames(QueueNames, value));

    private static IEnumerable<string> FilterNames(IEnumerable<string> names, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return names;

        var term = value.Trim();
        return names.Where(n => n.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static string ExchangeDisplay(string? name)
        => string.IsNullOrEmpty(name) ? "(default)" : name;

    private static string ExchangeName(MessageExchangeInfo? exchange)
        => exchange is null ? "" : ExchangeDisplay(exchange.Name);
}
