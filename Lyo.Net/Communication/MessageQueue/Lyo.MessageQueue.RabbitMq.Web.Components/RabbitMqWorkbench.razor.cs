using System.Text;
using System.Text.Json;
using Lyo.Common.Records;
using Lyo.Health;
using Lyo.MessageQueue;
using Lyo.MessageQueue.RabbitMq;
using Lyo.Web.Components.Dialog;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.MessageQueue.RabbitMq.Web.Components;

public partial class RabbitMqWorkbench : IAsyncDisposable
{
    [Inject]
    private IRabbitMqService MqService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    private bool _busy;
    private readonly List<KeyValuePair<string, string>> _details = [];
    private string _envelopeMessageId = string.Empty;
    private string _envelopeTraceId = string.Empty;
    private string _exchangeName = string.Empty;
    private List<MessageExchangeInfo> _exchangeStats = [];
    private HealthResult? _lastHealth;
    private string _messageBody = """{"message":"hello from lyo gateway"}""";
    private int _messagePriority;
    private int _peekCount = 10;
    private List<QueuePeekMessage> _peekedMessages = [];
    private string _queueName = string.Empty;
    private List<MessageQueueInfo> _queueStats = [];
    private string _routingKey = string.Empty;
    private double _sendDelaySeconds;
    private bool _showDefaultExchanges;
    private bool _statsAutoRefresh;
    private int _statsRefreshIntervalSeconds = 5;
    private CancellationTokenSource? _statsRefreshCts;
    private string _statusMessage = string.Empty;
    private bool _wrapInEnvelope;
    private Severity _statusSeverity = Severity.Info;
    private int _statusVersion;
    private int _tabIndex;
    private bool _exchangesLoaded;
    private bool _managementOk;

    /// <summary>AMQP is up, or the management API just answered — listing queues does not require an AMQP connection.</summary>
    private bool IsBrokerConnected => MqService.IsConnected() || _managementOk;

    private IReadOnlyList<string> BindableExchangeNames
        => _exchangeStats.Where(e => !string.IsNullOrEmpty(e.Name)).Select(e => e.Name).Distinct(StringComparer.Ordinal).ToList();

    private IReadOnlyList<string> BindableQueueNames
        => _queueStats.Select(q => q.Name).ToList();

    protected override async Task OnInitializedAsync()
    {
        await TryConnectQuietAsync();
        await RefreshQueueStatsAsync(false);
        await RefreshExchangesAsync(false);
    }

    private async Task OnTabChanged(int index)
    {
        _tabIndex = index;
        if (index == 1 && !_exchangesLoaded)
            await RefreshExchangesAsync(false);
    }

    private async Task CheckHealthAsync()
    {
        _busy = true;
        try {
            _lastHealth = await MqService.CheckHealthAsync();
            SetDetails("Health Check", _lastHealth.IsHealthy, [Detail("Healthy", _lastHealth.IsHealthy.ToString()), Detail("Duration", _lastHealth.Duration.ToString()), Detail("Message", _lastHealth.Message ?? "(none)"), Detail("Checked", _lastHealth.CheckedAt.ToString("u")), Detail("Exception", _lastHealth.Exception?.Message ?? "(none)"), .. _lastHealth.Metadata?.Select(x => Detail(x.Key, x.Value?.ToString() ?? "(null)")).ToList() ?? []]);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task OpenCreateQueueDialogAsync()
    {
        var dialog = await DialogService.ShowAsync<RabbitMqCreateQueueDialog>("Create queue", LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is not { Canceled: false, Data: RabbitMqCreateQueueRequest request })
            return;

        if (!await EnsureConnectedAsync())
            return;

        _busy = true;
        try {
            IDictionary<string, object>? arguments = request.MaxPriority > 0 ? new Dictionary<string, object> { ["x-max-priority"] = request.MaxPriority } : null;
            bool ok;
            if (request.CreateWithDlq) {
                var dlqName = string.IsNullOrWhiteSpace(request.DlqName) ? null : request.DlqName.Trim();
                ok = await MqService.CreateQueueWithDlq(request.Name.Trim(), request.Durable, dlqName, arguments);
            }
            else
                ok = await MqService.CreateQueue(request.Name.Trim(), request.Durable, request.Exclusive, request.AutoDelete, arguments);

            _queueName = request.Name.Trim();
            SetDetails("Create Queue", ok, [
                Detail("Queue", _queueName),
                Detail("Durable", request.Durable.ToString()),
                Detail("Exclusive", request.Exclusive.ToString()),
                Detail("Auto Delete", request.AutoDelete.ToString()),
                Detail("With DLQ", request.CreateWithDlq.ToString()),
                Detail("DLQ Name", request.CreateWithDlq ? string.IsNullOrWhiteSpace(request.DlqName) ? $"{_queueName}.dlq" : request.DlqName.Trim() : "(none)"),
                Detail("Max Priority", request.MaxPriority > 0 ? request.MaxPriority.ToString() : "(none)")
            ]);
            await RefreshQueueStatsAsync(false);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task OpenCreateExchangeDialogAsync()
    {
        var dialog = await DialogService.ShowAsync<RabbitMqCreateExchangeDialog>("Create exchange", LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is not { Canceled: false, Data: RabbitMqCreateExchangeRequest request })
            return;

        if (!await EnsureConnectedAsync())
            return;

        _busy = true;
        try {
            var ok = await MqService.CreateExchange(request.Name.Trim(), request.Type, request.Durable, request.AutoDelete);
            _exchangeName = request.Name.Trim();
            SetDetails("Create Exchange", ok, [Detail("Exchange", _exchangeName), Detail("Type", request.Type), Detail("Durable", request.Durable.ToString()), Detail("Auto Delete", request.AutoDelete.ToString())]);
            await RefreshExchangesAsync(false);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task BindQueueAsync()
    {
        if (!await EnsureConnectedAsync())
            return;

        if (string.IsNullOrWhiteSpace(_exchangeName)) {
            SetStatus("Enter an exchange name.", Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_queueName)) {
            SetStatus("Enter a queue name.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            var ok = await MqService.BindQueueToExchange(_queueName, _exchangeName, _routingKey);
            SetDetails("Bind Queue", ok, [Detail("Queue", _queueName), Detail("Exchange", _exchangeName), Detail("Routing Key", _routingKey)]);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task ClearQueueAsync(MessageQueueInfo queue)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync("Clear queue", $"Purge all messages from '{queue.Name}'?", yesText: "Clear", cancelText: "Cancel");
        if (confirmed != true)
            return;

        if (!await EnsureConnectedAsync())
            return;

        _queueName = queue.Name;
        _busy = true;
        try {
            var ok = await MqService.ClearQueue(_queueName);
            SetDetails("Clear Queue", ok, [Detail("Queue", _queueName)]);
            await RefreshQueueStatsAsync(false);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task DeleteQueueAsync(MessageQueueInfo queue)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync("Delete queue", $"Delete queue '{queue.Name}'?", yesText: "Delete", cancelText: "Cancel");
        if (confirmed != true)
            return;

        if (!await EnsureConnectedAsync())
            return;

        _busy = true;
        try {
            var ok = await MqService.DeleteQueue(queue.Name);
            SetDetails("Delete Queue", ok, [Detail("Queue", queue.Name)]);
            if (string.Equals(_queueName, queue.Name, StringComparison.Ordinal))
                _queueName = string.Empty;

            await RefreshQueueStatsAsync(false);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task DeleteExchangeAsync(MessageExchangeInfo exchange)
    {
        if (RabbitMqColorHelper.IsDefaultExchange(exchange.Name)) {
            SetStatus("Broker default exchanges cannot be deleted.", Severity.Warning);
            return;
        }

        var confirmed = await DialogService.ShowMessageBoxAsync("Delete exchange", $"Delete exchange '{exchange.Name}'?", yesText: "Delete", cancelText: "Cancel");
        if (confirmed != true)
            return;

        if (!await EnsureConnectedAsync())
            return;

        _busy = true;
        try {
            var ok = await MqService.DeleteExchange(exchange.Name);
            SetDetails("Delete Exchange", ok, [Detail("Exchange", exchange.Name)]);
            if (string.Equals(_exchangeName, exchange.Name, StringComparison.Ordinal))
                _exchangeName = string.Empty;

            await RefreshExchangesAsync(false);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task SendToQueueAsync()
    {
        if (!await EnsureConnectedAsync())
            return;

        _busy = true;
        try {
            var ok = await PublishToQueueAsync(false);
            if (ok.HasValue) {
                SetPublishDetails("Send To Queue", ok.Value);
                await RefreshQueueStatsAsync(false);
            }
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task SendDelayedToQueueAsync()
    {
        if (!await EnsureConnectedAsync())
            return;

        if (_sendDelaySeconds <= 0) {
            SetStatus("Enter a delay greater than zero seconds, or use Send to queue for immediate delivery.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            var ok = await PublishToQueueAsync(true);
            if (ok.HasValue) {
                SetPublishDetails("Send Delayed To Queue", ok.Value);
                await RefreshQueueStatsAsync(false);
            }
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    /// <summary>Publishes to the selected queue using the current envelope, priority, and optional delay settings.</summary>
    private async Task<bool?> PublishToQueueAsync(bool useDelay)
    {
        if (!TryBuildPublishBytes(out var bytes, out var buildError)) {
            SetStatus(buildError!, Severity.Warning);
            return null;
        }

        if (useDelay) {
            if (_messagePriority > 0)
                SetStatus("Delayed publish does not set message priority; priority is ignored for this send.", Severity.Info);

            var delay = TimeSpan.FromSeconds(_sendDelaySeconds);
            return await MqService.SendToQueueDelayed(_queueName, bytes, delay);
        }

        if (_wrapInEnvelope) {
            var json = string.IsNullOrWhiteSpace(_messageBody) ? "{}" : _messageBody;
            var payload = JsonSerializer.Deserialize<JsonElement>(json);
            var messageId = string.IsNullOrWhiteSpace(_envelopeMessageId) ? Guid.NewGuid().ToString("D") : _envelopeMessageId.Trim();
            var traceId = string.IsNullOrWhiteSpace(_envelopeTraceId) ? null : _envelopeTraceId.Trim();
            return await MqService.SendToQueueWithEnvelopeAsync(_queueName, payload, messageId: messageId, traceId: traceId, priority: (byte)Math.Clamp(_messagePriority, 0, 255));
        }

        if (_messagePriority > 0)
            return await MqService.SendToQueueWithPriority(_queueName, bytes, (byte)Math.Clamp(_messagePriority, 0, 255));

        return await MqService.SendToQueue(_queueName, bytes);
    }

    private void SetPublishDetails(string operation, bool ok)
    {
        if (!TryBuildPublishBytes(out var bytes, out var _))
            bytes = [];

        SetDetails(operation, ok, [Detail("Queue", _queueName), Detail("QueueMessageEnvelope", _wrapInEnvelope.ToString()), Detail("Priority", _messagePriority > 0 ? _messagePriority.ToString() : "(default)"), Detail("Delay", operation.Contains("Delayed", StringComparison.Ordinal) ? $"{_sendDelaySeconds}s" : "(none)"), Detail("Payload Size", FileSizeUnitInfo.FormatBestFitAbbreviation(bytes.LongLength, lowercaseAbbreviation: false))]);
    }

    private async Task SendToExchangeAsync()
    {
        if (!await EnsureConnectedAsync())
            return;

        if (string.IsNullOrWhiteSpace(_exchangeName)) {
            SetStatus("Enter an exchange name.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            if (!TryBuildPublishBytes(out var bytes, out var buildError)) {
                SetStatus(buildError!, Severity.Warning);
                return;
            }

            var ok = await MqService.SendToExchange(_exchangeName, _routingKey, bytes);
            SetDetails("Send To Exchange", ok, [Detail("Exchange", _exchangeName), Detail("Routing Key", _routingKey), Detail("QueueMessageEnvelope", _wrapInEnvelope.ToString()), Detail("Priority", _messagePriority > 0 ? _messagePriority.ToString() : "(default)"), Detail("Payload Size", FileSizeUnitInfo.FormatBestFitAbbreviation(bytes.LongLength, lowercaseAbbreviation: false))]);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private Task SendDelayedToExchangeAsync()
    {
        SetStatus("Delayed publish is queue-only. Send to a queue, or bind a queue and use Send delayed there.", Severity.Warning);
        return Task.CompletedTask;
    }

    private async Task PeekQueueAsync(MessageQueueInfo queue)
    {
        _queueName = queue.Name;
        _busy = true;
        try {
            _peekedMessages = (await MqService.PeekQueueMessages(_queueName, _peekCount)).ToList();
            SetDetails("Peek Messages", true, [Detail("Queue", _queueName), Detail("Requested", _peekCount.ToString()), Detail("Returned", _peekedMessages.Count.ToString()), Detail("Ack Mode", "Requeue (no ack/remove)")]);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task RefreshQueueStatsAsync(bool updateStatus = true)
    {
        if (!_statsAutoRefresh)
            _busy = true;

        try {
            _queueStats = (await MqService.GetAllQueuesInfoAsync()).ToList();
            _managementOk = true;
            if (updateStatus)
                SetDetails("Queue Stats", true, [Detail("Queues", _queueStats.Count.ToString()), Detail("Total Messages", _queueStats.Sum(q => q.Messages).ToString()), Detail("Total Consumers", _queueStats.Sum(q => q.Consumers).ToString()), Detail("Auto Refresh", _statsAutoRefresh ? $"every {_statsRefreshIntervalSeconds}s" : "off")]);
        }
        catch (Exception ex) {
            _managementOk = false;
            if (updateStatus)
                SetStatus(ex.Message, Severity.Error);
        }
        finally {
            if (!_statsAutoRefresh)
                _busy = false;
        }
    }

    private async Task RefreshExchangesAsync(bool updateStatus = true)
    {
        _busy = true;
        try {
            _exchangeStats = (await MqService.GetAllExchangesInfoAsync()).ToList();
            _exchangesLoaded = true;
            if (updateStatus)
                SetDetails("Exchange Stats", true, [Detail("Exchanges", _exchangeStats.Count.ToString()), Detail("Custom", _exchangeStats.Count(e => !RabbitMqColorHelper.IsDefaultExchange(e.Name)).ToString())]);
        }
        catch (Exception ex) {
            if (updateStatus)
                SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task OnStatsAutoRefreshChanged(bool enabled)
    {
        _statsAutoRefresh = enabled;
        await StopStatsAutoRefreshAsync();
        if (!enabled)
            return;

        _statsRefreshCts = new();
        _ = RunStatsAutoRefreshAsync(_statsRefreshCts.Token);
    }

    private async Task RunStatsAutoRefreshAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(2, _statsRefreshIntervalSeconds)));
        try {
            await RefreshQueueStatsAsync(false);
            await InvokeAsync(StateHasChanged);
            while (await timer.WaitForNextTickAsync(ct)) {
                await RefreshQueueStatsAsync(false);
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException) {
            // Expected when auto refresh is turned off or the component is disposed.
        }
    }

    private async Task StopStatsAutoRefreshAsync()
    {
        if (_statsRefreshCts is null)
            return;

        await _statsRefreshCts.CancelAsync();
        _statsRefreshCts.Dispose();
        _statsRefreshCts = null;
    }

    public async ValueTask DisposeAsync() => await StopStatsAutoRefreshAsync();

    private async Task TryConnectQuietAsync()
    {
        if (MqService.IsConnected())
            return;

        try {
            await MqService.ConnectAsync();
        }
        catch {
            // Listing still uses the HTTP management API; the chip falls back to _managementOk.
        }
    }

    private async Task<bool> EnsureConnectedAsync()
    {
        if (MqService.IsConnected())
            return true;

        try {
            await MqService.ConnectAsync();
            return true;
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
            return false;
        }
    }

    private void SetDetails(string operation, bool ok, IEnumerable<KeyValuePair<string, string>> details)
    {
        _details.Clear();
        _details.Add(Detail("Operation", operation));
        _details.Add(Detail("Success", ok.ToString()));
        _details.AddRange(details);
        SetStatus($"{operation} {(ok ? "succeeded" : "failed")}.", ok ? Severity.Success : Severity.Error);
    }

    private static KeyValuePair<string, string> Detail(string key, string value) => new(key, value);

    private bool TryBuildPublishBytes(out byte[] bytes, out string? error)
    {
        if (!_wrapInEnvelope) {
            bytes = Encoding.UTF8.GetBytes(_messageBody);
            error = null;
            return true;
        }

        try {
            var json = string.IsNullOrWhiteSpace(_messageBody) ? "{}" : _messageBody;
            var payload = JsonSerializer.Deserialize<JsonElement>(json);
            var messageId = string.IsNullOrWhiteSpace(_envelopeMessageId) ? Guid.NewGuid().ToString("D") : _envelopeMessageId.Trim();
            var traceId = string.IsNullOrWhiteSpace(_envelopeTraceId) ? null : _envelopeTraceId.Trim();
            var envelope = new QueueMessageEnvelope<JsonElement>(payload, 0, messageId, DateTime.UtcNow, traceId);
            bytes = JsonSerializer.SerializeToUtf8Bytes(envelope);
            error = null;
            return true;
        }
        catch (JsonException ex) {
            bytes = [];
            error = $"QueueMessageEnvelope mode requires valid JSON in the message body: {ex.Message}";
            return false;
        }
    }

    private void SetStatus(string message, Severity severity)
    {
        _statusMessage = message;
        _statusSeverity = severity;
        var version = ++_statusVersion;
        Snackbar.Add(message, severity);
        _ = ClearStatusLaterAsync(version);
    }

    private async Task ClearStatusLaterAsync(int version)
    {
        await Task.Delay(2500);
        if (version != _statusVersion)
            return;

        _statusMessage = string.Empty;
        await InvokeAsync(StateHasChanged);
    }
}
