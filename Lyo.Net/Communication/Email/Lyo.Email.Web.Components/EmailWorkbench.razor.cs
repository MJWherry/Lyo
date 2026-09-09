using System.Net.Http.Json;
using Lyo.Email.Builders;
using Lyo.Email.Models;
using Lyo.Result;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Lyo.Web.Components.FileUpload;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Email.Web.Components;

public partial class EmailWorkbench
{
    private sealed record EmailAttachment(string FileName, byte[] Content);

    private bool _busy;
    private readonly List<EmailAttachment> _emailAttachments = [];
    private readonly string _emailValidationPattern = RegexPatterns.EmailRegex.ToString();
    private List<string> _emailBccValues = [];
    private List<string> _emailCcValues = [];
    private string _emailFromAddress = string.Empty;
    private string? _emailFromName;
    private string _emailHtmlBody = "<p>Hello from Lyo.</p>";
    private Result<EmailRequest>? _lastEmailResult;
    private EmailResult? _lastEmailProviderResult;
    private string _lastEmailSummary = string.Empty;
    private string _emailSubject = "Lyo test message";
    private string _emailTextBody = "<p>Hello from Lyo.</p>";
    private List<string> _emailToValues = ["test@example.com"];
    private string _attachmentUploadStatus = "No attachment uploads yet.";
    private readonly List<string> _attachmentUploadLog = [];

    private Task OnEmailAttachmentReadyAsync(LocalBrowserFile file)
    {
        _emailAttachments.RemoveAll(x => x.FileName == file.FileName);
        _emailAttachments.Add(new(file.FileName, file.Content.ToArray()));
        SetStatus($"Loaded {_emailAttachments.Count} attachment(s).", Severity.Success);
        return Task.CompletedTask;
    }

    private Task OnEmailAttachmentRemovedAsync(LocalBrowserFile file)
    {
        _emailAttachments.RemoveAll(x => x.FileName == file.FileName);
        SetStatus(_emailAttachments.Count == 0 ? "All attachments removed." : $"{_emailAttachments.Count} attachment(s) remain.", Severity.Info);
        return Task.CompletedTask;
    }

    private Task OnAttachmentUploadStartedAsync(LyoFileUploadEventArgs args) => UpdateAttachmentUploadAsync($"Uploading {args.FileName}...", $"Started {args.FileName}");

    private Task OnAttachmentUploadProgressAsync(LyoFileUploadEventArgs args)
    {
        _attachmentUploadStatus = $"{args.FileName}: {args.Progress:F0}%";
        return Task.CompletedTask;
    }

    private Task OnAttachmentUploadCompletedAsync(LyoFileUploadEventArgs args) => UpdateAttachmentUploadAsync($"{args.FileName} completed.", $"Completed {args.FileName}");

    private Task OnAttachmentUploadCancelledAsync(LyoFileUploadEventArgs args) => UpdateAttachmentUploadAsync($"{args.FileName} cancelled.", $"Cancelled {args.FileName}");

    private Task OnAttachmentUploadFailedAsync(LyoFileUploadEventArgs args) => UpdateAttachmentUploadAsync($"{args.FileName} failed: {args.ErrorMessage}", $"Failed {args.FileName}");

    private async Task SendEmailAsync()
    {
        if (_emailToValues.Count == 0) {
            SetStatus("Add at least one email recipient.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            if (!string.Equals(_emailHtmlBody, _emailTextBody, StringComparison.Ordinal))
                _emailHtmlBody = _emailTextBody;

            var builder = EmailRequestBuilder.New().SetSubject(_emailSubject);
            foreach (var recipient in _emailToValues)
                builder.AddTo(recipient);

            foreach (var cc in _emailCcValues)
                builder.AddCc(cc);

            foreach (var bcc in _emailBccValues)
                builder.AddBcc(bcc);

            if (!string.IsNullOrWhiteSpace(_emailHtmlBody))
                builder.SetHtmlBody(_emailHtmlBody);

            if (!string.IsNullOrWhiteSpace(_emailTextBody))
                builder.SetTextBody(_emailTextBody);

            foreach (var attachment in _emailAttachments)
                builder.AddAttachment(attachment.FileName, attachment.Content);

            _lastEmailResult = string.IsNullOrWhiteSpace(_emailFromAddress) ? await EmailService.SendEmailAsync(builder) : await EmailService.SendEmailAsync(builder, _emailFromAddress, _emailFromName);
            _lastEmailProviderResult = _lastEmailResult as EmailResult;
            _lastEmailSummary = _lastEmailResult.IsSuccess ? $"Email sent to {_emailToValues.Count} recipient(s)." : LyoResultErrorFormatter.FormatErrors(_lastEmailResult.Errors);
            SetStatus(_lastEmailSummary, _lastEmailResult.IsSuccess ? Severity.Success : Severity.Error);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task TestEmailConnectionAsync()
    {
        _busy = true;
        try {
            var ok = await EmailService.TestConnectionAsync();
            SetStatus(ok ? "Email connection succeeded." : "Email connection failed.", ok ? Severity.Success : Severity.Warning);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private Task OnEmailToValuesChanged(IEnumerable<string> values)
    {
        _emailToValues = values.ToList();
        return Task.CompletedTask;
    }

    private Task OnEmailCcValuesChanged(IEnumerable<string> values)
    {
        _emailCcValues = values.ToList();
        return Task.CompletedTask;
    }

    private Task OnEmailBccValuesChanged(IEnumerable<string> values)
    {
        _emailBccValues = values.ToList();
        return Task.CompletedTask;
    }

    private Task OnEmailHtmlBodyChangedAsync(string html)
    {
        _emailHtmlBody = html;
        _emailTextBody = html;
        return Task.CompletedTask;
    }

    private Task OnEmailTextBodyChangedAsync(string text)
    {
        _emailTextBody = text;
        _emailHtmlBody = text;
        return Task.CompletedTask;
    }

    private Task UpdateAttachmentUploadAsync(string status, string logEntry)
    {
        _attachmentUploadStatus = status;
        _attachmentUploadLog.Insert(0, $"{DateTime.Now:HH:mm:ss} {logEntry}");
        if (_attachmentUploadLog.Count > 6)
            _attachmentUploadLog.RemoveAt(_attachmentUploadLog.Count - 1);

        return Task.CompletedTask;
    }
}
