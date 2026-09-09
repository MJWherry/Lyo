using System.Net.Http.Json;
using Lyo.Result;
using Lyo.Sms.Builders;
using Lyo.Sms.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Sms.Web.Components;

public partial class SmsWorkbench
{
    private bool _busy;
    private readonly string _phoneValidationPattern = RegexPatterns.PhoneNumberRegex.ToString();
    private readonly string _publicMediaUrlValidationPattern = @"^https?:\/\/[^\s]+$";
    private BulkResult<SmsRequest>? _lastSmsBulkResult;
    private List<Result<SmsRequest>> _lastSmsResults = [];
    private string _lastSmsSummary = string.Empty;
    private string _smsBody = "Hello from Lyo.";
    private string? _smsFrom;
    private List<string> _smsMediaUrlValues = [];
    private List<string> _smsToValues = ["+15551234567"];

    private async Task SendSmsAsync()
    {
        if (_smsToValues.Count == 0) {
            SetStatus("Add at least one phone number.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            var bulkBuilder = BulkSmsBuilder.New();
            if (!string.IsNullOrWhiteSpace(_smsFrom))
                bulkBuilder.SetDefaultFrom(_smsFrom);

            foreach (var recipient in _smsToValues) {
                bulkBuilder.Add(recipient, _smsBody);
                foreach (var mediaUrl in _smsMediaUrlValues)
                    bulkBuilder.AddAttachment(mediaUrl);
            }

            _lastSmsBulkResult = await SmsService.SendBulkAsync(bulkBuilder);
            _lastSmsResults = _lastSmsBulkResult.Results.ToList();
            _lastSmsSummary = _lastSmsBulkResult.IsCompleteSuccess ? $"SMS completed: {_lastSmsBulkResult.SuccessCount}/{_lastSmsBulkResult.TotalCount} succeeded." : string.Join(Environment.NewLine, _lastSmsBulkResult.ErrorMessages);
            SetStatus(_lastSmsSummary, _lastSmsBulkResult.IsCompleteSuccess ? Severity.Success : Severity.Error);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task TestSmsConnectionAsync()
    {
        _busy = true;
        try {
            var ok = await SmsService.TestConnectionAsync();
            SetStatus(ok ? "SMS connection succeeded." : "SMS connection failed.", ok ? Severity.Success : Severity.Warning);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private Task OnSmsToValuesChanged(IEnumerable<string> values)
    {
        _smsToValues = values.ToList();
        return Task.CompletedTask;
    }

    private Task OnSmsMediaUrlValuesChanged(IEnumerable<string> values)
    {
        _smsMediaUrlValues = values.ToList();
        return Task.CompletedTask;
    }
}
