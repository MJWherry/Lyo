using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

#if NETSTANDARD2_0
#pragma warning disable CS8604 // Possible null reference argument.
#endif

namespace Lyo.Email.Builders;

/// <summary>Fluent helper for a bulk send that shares a default sender.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class BulkEmailRequestBuilder
{
    private readonly List<MessageEntry> _messages = [];

    private string? _defaultFromAddress;

    private string? _defaultFromName;

    private int? _maxLimit;

    /// <summary>How many messages are in this batch.</summary>
    public int Count => _messages.Count;

    /// <summary>Caps how many messages this batch may hold.</summary>
    /// <param name="maxLimit">Maximum message count (must be greater than 0).</param>
    /// <returns>This builder for further calls.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when maxLimit is outside [1, int.MaxValue].</exception>
    public BulkEmailRequestBuilder SetMaxLimit(int maxLimit)
    {
        ArgumentHelpers.ThrowIfNullOrNotInRange(maxLimit, 1, int.MaxValue);
        _maxLimit = maxLimit;
        return this;
    }

    /// <summary>Sets the default sender used by every message in this batch.</summary>
    /// <param name="fromAddress">Default sender address.</param>
    /// <param name="fromName">Optional sender display name.</param>
    /// <returns>This builder for further calls.</returns>
    /// <exception cref="ArgumentException">Thrown when the address is null or empty.</exception>
    public BulkEmailRequestBuilder SetDefaultFrom(string fromAddress, string? fromName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(fromAddress);
        _defaultFromAddress = fromAddress;
        _defaultFromName = fromName;
        return this;
    }

    /// <summary>Adds one message to the batch.</summary>
    /// <param name="to">Recipient address (required).</param>
    /// <param name="subject">Subject (required).</param>
    /// <param name="textBody">Plain-text body (optional when htmlBody is set).</param>
    /// <param name="htmlBody">HTML body (optional when textBody is set).</param>
    /// <returns>This builder for further calls.</returns>
    /// <exception cref="ArgumentException">Thrown when arguments are invalid.</exception>
    public BulkEmailRequestBuilder Add(string to, string subject, string? textBody = null, string? htmlBody = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(to);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(subject);
        ArgumentHelpers.ThrowIf(
            _maxLimit.HasValue && _messages.Count >= _maxLimit.Value,
            $"Cannot add more messages: current count ({_messages.Count}) would exceed maximum limit ({_maxLimit!.Value}).", nameof(to));

        ArgumentHelpers.ThrowIf(string.IsNullOrWhiteSpace(textBody) && string.IsNullOrWhiteSpace(htmlBody), "Either textBody or htmlBody must be provided.", nameof(textBody));
        _messages.Add(
            new() {
                To = to,
                Subject = subject,
                TextBody = textBody,
                HtmlBody = htmlBody,
                FromAddress = null,
                FromName = null,
                CcAddresses = [],
                BccAddresses = []
            });

        return this;
    }

    /// <summary>Adds a message with its own sender, overriding the batch default.</summary>
    /// <param name="to">Recipient address (required).</param>
    /// <param name="subject">Subject (required).</param>
    /// <param name="textBody">Plain-text body (optional when htmlBody is set).</param>
    /// <param name="htmlBody">HTML body (optional when textBody is set).</param>
    /// <param name="fromAddress">Sender address; the batch default is used when omitted.</param>
    /// <param name="fromName">Optional sender display name.</param>
    /// <returns>This builder for further calls.</returns>
    /// <exception cref="ArgumentException">Thrown when arguments are invalid.</exception>
    public BulkEmailRequestBuilder Add(string to, string subject, string? textBody, string? htmlBody, string? fromAddress, string? fromName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(to);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(subject);
        ArgumentHelpers.ThrowIf(
            _maxLimit.HasValue && _messages.Count >= _maxLimit.Value,
            $"Cannot add more messages: current count ({_messages.Count}) would exceed maximum limit ({_maxLimit!.Value}).", nameof(to));

        ArgumentHelpers.ThrowIf(string.IsNullOrWhiteSpace(textBody) && string.IsNullOrWhiteSpace(htmlBody), "Either textBody or htmlBody must be provided.", nameof(textBody));
        _messages.Add(
            new() {
                To = to,
                Subject = subject,
                TextBody = textBody,
                HtmlBody = htmlBody,
                FromAddress = fromAddress,
                FromName = fromName,
                CcAddresses = [],
                BccAddresses = []
            });

        return this;
    }

    /// <summary>Adds a CC address to the most recently added message.</summary>
    /// <param name="cc">CC address.</param>
    /// <returns>This builder for further calls.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the batch has no messages yet.</exception>
    public BulkEmailRequestBuilder AddCc(string cc)
    {
        OperationHelpers.ThrowIfZero(_messages.Count, "Cannot add CC: No messages have been added yet. Call Add() first.");
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(cc);
        _messages[^1].CcAddresses.Add(cc);
        return this;
    }

    /// <summary>Adds a BCC address to the most recently added message.</summary>
    /// <param name="bcc">BCC address.</param>
    /// <returns>This builder for further calls.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the batch has no messages yet.</exception>
    public BulkEmailRequestBuilder AddBcc(string bcc)
    {
        OperationHelpers.ThrowIfZero(_messages.Count, "Cannot add BCC: No messages have been added yet. Call Add() first.");
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(bcc);
        _messages[^1].BccAddresses.Add(bcc);
        return this;
    }

    /// <summary>Removes every message and the default sender.</summary>
    /// <returns>This builder for further calls.</returns>
    public BulkEmailRequestBuilder Clear()
    {
        _messages.Clear();
        _defaultFromAddress = null;
        _defaultFromName = null;
        _maxLimit = null;
        return this;
    }

    /// <summary>Materializes EmailBuilder instances from this bulk builder.</summary>
    /// <returns>The built EmailBuilder collection.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the batch is empty.</exception>
    public IEnumerable<EmailRequestBuilder> Build()
    {
        OperationHelpers.ThrowIfZero(_messages.Count, "No messages have been added to the bulk send");
        foreach (var message in _messages) {
            var builder = EmailRequestBuilder.New().SetSubject(message.Subject).AddTo(message.To);

            // Prefer a per-message sender; otherwise the batch default
            if (!string.IsNullOrWhiteSpace(message.FromAddress))
                builder.SetFrom(message.FromAddress, message.FromName);
            else if (!string.IsNullOrWhiteSpace(_defaultFromAddress))
                builder.SetFrom(_defaultFromAddress, _defaultFromName);
            // With no sender set, the service applies its own default

            // Apply body (HTML wins when both are present)
            if (!string.IsNullOrWhiteSpace(message.HtmlBody)) {
                builder.SetHtmlBody(message.HtmlBody);
                if (!string.IsNullOrWhiteSpace(message.TextBody))
                    builder.SetTextBody(message.TextBody);
            }
            else if (!string.IsNullOrWhiteSpace(message.TextBody))
                builder.SetTextBody(message.TextBody);

            // Attach CC and BCC
            foreach (var cc in message.CcAddresses)
                builder.AddCc(cc);

            foreach (var bcc in message.BccAddresses)
                builder.AddBcc(bcc);

            yield return builder;
        }
    }

    /// <summary>Starts a new BulkEmailBuilder.</summary>
    /// <returns>A fresh builder.</returns>
    public static BulkEmailRequestBuilder New() => new();

    /// <summary>Diagnostic snapshot of the bulk builder.</summary>
    /// <returns>Text covering message count and the default sender.</returns>
    public override string ToString() => $"BulkEmail: {_messages.Count} messages, DefaultFrom={_defaultFromAddress ?? "(not set)"}";

    private class MessageEntry
    {
        /// <summary>Primary recipient address.</summary>
        public string To { get; set; } = null!;

        /// <summary>Subject line.</summary>
        public string Subject { get; set; } = null!;

        /// <summary>Plain-text body.</summary>
        public string? TextBody { get; set; }

        /// <summary>HTML body.</summary>
        public string? HtmlBody { get; set; }

        /// <summary>Sender address for this message.</summary>
        public string? FromAddress { get; set; }

        /// <summary>Sender display name for this message.</summary>
        public string? FromName { get; set; }

        /// <summary>CC recipients for this message.</summary>
        public List<string> CcAddresses { get; set; } = [];

        /// <summary>BCC recipients for this message.</summary>
        public List<string> BccAddresses { get; set; } = [];
    }
}