using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Query.Models.Builders;
using SortDirection = Lyo.Common.Enums.SortDirection;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Result;
using Lyo.Sms;
using Lyo.Sms.Models;
using Lyo.Web.Components;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lyo.Sms.Web.Components;

/// <summary>Projected Twilio SMS log grid with an inline conversation panel between the two numbers on a row.</summary>
public partial class TwilioSmsLogGrid
{
    private const int ChatPageSize = 200;

    private static readonly string[] ChatSelectFields = [
        "Id", "To", "From", "Body", "Direction", "Status", "DateSent", "CreatedTimestamp", "MediaUrlsJson"
    ];

    private readonly DialogOptions _chatDialogOptions = new() {
        MaxWidth = MaxWidth.Small,
        FullWidth = true,
        CloseButton = true,
        CloseOnEscapeKey = true
    };

    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [
        new("To"), new("From"), new("Status"), new("Direction"), new("IsSuccess", "Success"), new("Body")
    ];

    private bool _chatBusy;
    private bool _chatOpen;
    private LyoDataGridProjected? _dataGrid;
    private string _draft = string.Empty;
    private string? _localNumber;
    private string? _peer;
    private string? _peerQuery;
    private string? _chatError;
    private List<ChatMessage> _thread = [];

    [Inject]
    private ISmsService SmsService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    /// <summary>API client used for QueryProject reads and log-row create after send.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Dynamic CRUD base route for the Twilio SMS DbContext (default: <c>Twilio</c>).</summary>
    [Parameter]
    public string BaseRoute { get; set; } = "Twilio";

    private string _logRoute => $"{BaseRoute.TrimEnd('/')}/TwilioSmsLogEntity";

    private string _queryProjectRoute => $"{_logRoute}/QueryProject";

    private static object[] GetKey(object? item)
    {
        if (item == null)
            return [];

        var id = ProjectedValueHelper.GetValue(item, "Id");
        return id != null ? [id] : [];
    }

    private async Task OpenChatAsync(object? item)
    {
        var (peer, local, peerRaw) = ResolveParticipants(item);
        if (string.IsNullOrWhiteSpace(peer) && string.IsNullOrWhiteSpace(peerRaw)) {
            Snackbar.Add("This row has no phone number to open a conversation.", Severity.Warning);
            return;
        }

        _peer = peer ?? peerRaw;
        _peerQuery = FirstNonEmpty(peerRaw, peer);
        _localNumber = local;
        _draft = string.Empty;
        _chatError = null;
        _thread = [];
        _chatOpen = true;
        await RefreshChatAsync();
    }

    private async Task RefreshChatAsync()
    {
        _chatBusy = true;
        _chatError = null;
        try {
            await LoadThreadAsync();
        }
        catch (Exception ex) {
            _chatError = ex.Message;
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _chatBusy = false;
        }
    }

    private async Task LoadThreadAsync()
    {
        var person = FirstNonEmpty(_peerQuery, _peer);
        if (string.IsNullOrWhiteSpace(person)) {
            _thread = [];
            return;
        }

        var messages = new List<ChatMessage>();
        var query = ProjectionQueryReqBuilder.New()
            .SetPagination(0, ChatPageSize)
            .AddSelects(ChatSelectFields)
            .AddWhere(BuildPersonWhere(person, _peer))
            .AddSort("DateSent", SortDirection.Asc, 1)
            .AddSort("CreatedTimestamp", SortDirection.Asc, 2)
            .Build();

        while (true) {
            var result = await ApiClient.PostAsAsync<ProjectionQueryReq, ProjectedQueryRes<object?>>(_queryProjectRoute, query);
            if (result is not { IsSuccess: true })
                throw new InvalidOperationException(result?.Error?.Detail ?? "Failed to load messages.");

            foreach (var row in result.Items ?? []) {
                if (row != null)
                    messages.Add(ToMessage(row));
            }

            if (result.HasMore != true || result.Items is not { Count: > 0 })
                break;

            query = result.ToNextProjectionQueryRequest();
        }

        _thread = messages.OrderBy(m => m.At ?? DateTime.MinValue).ToList();
    }

    private static WhereClause BuildPersonWhere(string personNumber, string? normalized)
    {
        var values = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(personNumber))
            values.Add(personNumber.Trim());
        if (!string.IsNullOrWhiteSpace(normalized))
            values.Add(normalized.Trim());

        var children = new List<WhereClause>();
        foreach (var value in values) {
            children.Add(new ConditionClause("To", ComparisonOperatorEnum.Equals, value));
            children.Add(new ConditionClause("From", ComparisonOperatorEnum.Equals, value));
        }

        return children.Count == 1 ? children[0] : new GroupClause(GroupOperatorEnum.Or, children);
    }

    private async Task OnDraftKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
            await SendChatAsync();
    }

    private async Task SendChatAsync()
    {
        if (_chatBusy || string.IsNullOrWhiteSpace(_peer) || string.IsNullOrWhiteSpace(_draft))
            return;

        var body = _draft.Trim();
        if (body.Length == 0)
            return;

        _chatBusy = true;
        _chatError = null;
        try {
            var from = string.IsNullOrWhiteSpace(_localNumber) ? null : _localNumber;
            var result = await SmsService.SendSmsAsync(_peer, body, from);
            if (!result.IsSuccess) {
                _chatError = LyoResultErrorFormatter.FormatErrors(result.Errors);
                Snackbar.Add(_chatError, Severity.Error);
                return;
            }

            _draft = string.Empty;
            await PersistLogAsync(result, body);
            await LoadThreadAsync();
            if (_dataGrid != null)
                await _dataGrid.RefreshData();
        }
        catch (Exception ex) {
            _chatError = ex.Message;
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _chatBusy = false;
        }
    }

    private async Task PersistLogAsync(Result<SmsRequest> result, string body)
    {
        var now = DateTime.UtcNow;
        var id = GetResultProperty<string>(result, "MessageId");
        if (string.IsNullOrWhiteSpace(id))
            id = "LY" + Guid.NewGuid().ToString("N");
        else if (id.Length > 34)
            id = id[..34];

        var log = new SmsLogCreateBody {
            Id = id,
            To = result.Data?.To ?? _peer ?? string.Empty,
            From = result.Data?.From ?? _localNumber,
            Body = result.Data?.Body ?? body,
            IsSuccess = result.IsSuccess,
            ErrorMessage = result.Errors is { Count: > 0 } ? result.Errors[0].Message : null,
            Status = GetResultProperty<string>(result, "Status"),
            ErrorCode = GetResultProperty<int?>(result, "TwilioErrorCode"),
            DateCreated = GetResultProperty<DateTime?>(result, "DateCreated") ?? now,
            DateSent = GetResultProperty<DateTime?>(result, "DateSent") ?? now,
            CreatedTimestamp = result.Timestamp == default ? now : result.Timestamp,
            Direction = "Outbound",
            NumSegments = GetResultProperty<int?>(result, "NumSegments"),
            AccountSid = GetResultProperty<string>(result, "AccountSid"),
            Price = GetResultProperty<decimal?>(result, "Price"),
            PriceUnit = GetResultProperty<string>(result, "PriceUnit")
        };

        try {
            await ApiClient.PostAsAsync<SmsLogCreateBody, CreateResult<object?>>(_logRoute, log);
        }
        catch (Exception ex) {
            Snackbar.Add($"Sent, but the log row was not saved: {ex.Message}", Severity.Warning);
        }
    }

    private static (string? Peer, string? Local, string? PeerRaw) ResolveParticipants(object? item)
    {
        if (item == null)
            return (null, null, null);

        var toRaw = ProjectedValueHelper.GetDisplayValue(item, "To");
        var fromRaw = ProjectedValueHelper.GetDisplayValue(item, "From");
        var to = PhoneNumber.Normalize(toRaw);
        var from = PhoneNumber.Normalize(fromRaw);
        if (IsInbound(ProjectedValueHelper.GetDisplayValue(item, "Direction")))
            return (FirstNonEmpty(from, to), FirstNonEmpty(to, from), FirstNonEmpty(fromRaw, toRaw));

        return (FirstNonEmpty(to, from), FirstNonEmpty(from, to), FirstNonEmpty(toRaw, fromRaw));
    }

    private static ChatMessage ToMessage(object row)
    {
        var at = LyoDateTimeDisplay.ToDateTime(ProjectedValueHelper.GetValue(row, "DateSent"))
                 ?? LyoDateTimeDisplay.ToDateTime(ProjectedValueHelper.GetValue(row, "CreatedTimestamp"));
        return new(
            ProjectedValueHelper.GetDisplayValue(row, "Id"),
            ProjectedValueHelper.GetDisplayValue(row, "Body"),
            IsInbound(ProjectedValueHelper.GetDisplayValue(row, "Direction")),
            at,
            ParseMedia(ProjectedValueHelper.GetDisplayValue(row, "MediaUrlsJson")),
            ProjectedValueHelper.GetDisplayValue(row, "Status"));
    }

    private static bool IsInbound(string? direction)
        => !string.IsNullOrWhiteSpace(direction)
           && (direction.Equals("Inbound", StringComparison.OrdinalIgnoreCase) || direction == "1");

    private static IReadOnlyList<string> ParseMedia(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json is "[]" or "null")
            return [];

        try {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException) {
            return [];
        }
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values) {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static string FormatChatTime(DateTime? value)
    {
        if (value == null)
            return string.Empty;

        var utc = value.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value.Value;
        var local = utc.Kind == DateTimeKind.Utc ? utc.ToLocalTime() : utc;
        var today = DateTime.Now.Date;
        return local.Date == today
            ? local.ToString("t", CultureInfo.CurrentCulture)
            : local.ToString("g", CultureInfo.CurrentCulture);
    }

    private static T? GetResultProperty<T>(object result, string name)
    {
        var prop = result.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (prop == null)
            return default;

        var value = prop.GetValue(result);
        if (value is T typed)
            return typed;

        if (value == null)
            return default;

        try {
            var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T?)Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
        }
        catch (Exception) {
            return default;
        }
    }

    private sealed record ChatMessage(string Id, string Body, bool Inbound, DateTime? At, IReadOnlyList<string> MediaUrls, string Status);

    private sealed class SmsLogCreateBody
    {
        public string Id { get; set; } = string.Empty;

        public string To { get; set; } = string.Empty;

        public string? From { get; set; }

        public string? Body { get; set; }

        public bool IsSuccess { get; set; }

        public string? ErrorMessage { get; set; }

        public long ElapsedTimeMs { get; set; }

        public string? Status { get; set; }

        public int? ErrorCode { get; set; }

        public DateTime? DateCreated { get; set; }

        public DateTime? DateSent { get; set; }

        public DateTime CreatedTimestamp { get; set; }

        public string Direction { get; set; } = "Outbound";

        public int? NumSegments { get; set; }

        public string? AccountSid { get; set; }

        public decimal? Price { get; set; }

        public string? PriceUnit { get; set; }
    }
}
