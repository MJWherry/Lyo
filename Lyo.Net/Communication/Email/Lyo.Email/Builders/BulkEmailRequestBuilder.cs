using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
#if NETSTANDARD2_0
#pragma warning disable CS8604 // Possible null reference argument.
#endif

namespace Lyo.Email.Builders;

/// <summary>Builder class for constructing bulk email messages with a default sender.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class BulkEmailRequestBuilder
{
    private readonly List<MessageEntry> _messages = [];

    private string? _defaultFromAddress;

    private string? _defaultFromName;

    private int? _maxLimit;

    /// <summary>Gets the number of messages in this bulk send.</summary>
    public int Count => _messages.Count;

    /// <summary>Sets the maximum number of messages allowed in this bulk send.</summary>
    /// <param name="maxLimit">The maximum number of messages (must be greater than 0).</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when maxLimit is outside the range [1, int.MaxValue].</exception>
    public BulkEmailRequestBuilder SetMaxLimit(int maxLimit)
    {
        ArgumentHelpers.ThrowIfNullOrNotInRange(maxLimit, 1, int.MaxValue, nameof(maxLimit));
        _maxLimit = maxLimit;
        return this;
    }

    /// <summary>Sets the default sender email address and name for all messages in this bulk send.</summary>
    /// <param name="fromAddress">The default sender email address.</param>
    /// <param name="fromName">Optional display name for the sender.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when the email address is null or empty.</exception>
    public BulkEmailRequestBuilder SetDefaultFrom(string fromAddress, string? fromName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(fromAddress, nameof(fromAddress));
        _defaultFromAddress = fromAddress;
        _defaultFromName = fromName;
        return this;
    }

    /// <summary>Adds a message to the bulk send.</summary>
    /// <param name="to">The recipient email address (required).</param>
    /// <param name="subject">The email subject (required).</param>
    /// <param name="textBody">The plain text body (optional if htmlBody is provided).</param>
    /// <param name="htmlBody">The HTML body (optional if textBody is provided).</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public BulkEmailRequestBuilder Add(string to, string subject, string? textBody = null, string? htmlBody = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(to, nameof(to));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(subject, nameof(subject));
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
                CcAddresses = new(),
                BccAddresses = new()
            });

        return this;
    }

    /// <summary>Adds a message to the bulk send with a specific sender (overrides default).</summary>
    /// <param name="to">The recipient email address (required).</param>
    /// <param name="subject">The email subject (required).</param>
    /// <param name="textBody">The plain text body (optional if htmlBody is provided).</param>
    /// <param name="htmlBody">The HTML body (optional if textBody is provided).</param>
    /// <param name="fromAddress">The sender email address (optional, uses default if not provided).</param>
    /// <param name="fromName">The sender display name (optional).</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public BulkEmailRequestBuilder Add(string to, string subject, string? textBody, string? htmlBody, string? fromAddress, string? fromName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(to, nameof(to));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(subject, nameof(subject));
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
                CcAddresses = new(),
                BccAddresses = new()
            });

        return this;
    }

    /// <summary>Adds a CC recipient to the last message added.</summary>
    /// <param name="cc">The CC email address.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no messages have been added yet.</exception>
    public BulkEmailRequestBuilder AddCc(string cc)
    {
        if (_messages.Count == 0)
            OperationHelpers.ThrowIf(true, "Cannot add CC: No messages have been added yet. Call Add() first.");

        ArgumentHelpers.ThrowIfNullOrWhiteSpace(cc, nameof(cc));
        _messages[_messages.Count - 1].CcAddresses.Add(cc);
        return this;
    }

    /// <summary>Adds a BCC recipient to the last message added.</summary>
    /// <param name="bcc">The BCC email address.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no messages have been added yet.</exception>
    public BulkEmailRequestBuilder AddBcc(string bcc)
    {
        if (_messages.Count == 0)
            OperationHelpers.ThrowIf(true, "Cannot add BCC: No messages have been added yet. Call Add() first.");

        ArgumentHelpers.ThrowIfNullOrWhiteSpace(bcc, nameof(bcc));
        _messages[_messages.Count - 1].BccAddresses.Add(bcc);
        return this;
    }

    /// <summary>Clears all messages and the default sender.</summary>
    /// <returns>The builder instance for method chaining.</returns>
    public BulkEmailRequestBuilder Clear()
    {
        _messages.Clear();
        _defaultFromAddress = null;
        _defaultFromName = null;
        _maxLimit = null;
        return this;
    }

    /// <summary>Builds the collection of EmailBuilder instances from the bulk builder.</summary>
    /// <returns>Collection of EmailBuilder instances.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no messages are added.</exception>
    public IEnumerable<EmailRequestBuilder> Build()
    {
        if (_messages.Count == 0)
            OperationHelpers.ThrowIf(true, "No messages have been added to the bulk send");

        foreach (var message in _messages) {
            var builder = EmailRequestBuilder.New().SetSubject(message.Subject).AddTo(message.To);

            // Use message-specific sender if provided, otherwise use default
            if (!string.IsNullOrWhiteSpace(message.FromAddress))
                builder.SetFrom(message.FromAddress, message.FromName);
            else if (!string.IsNullOrWhiteSpace(_defaultFromAddress))
                builder.SetFrom(_defaultFromAddress, _defaultFromName);
            // If no sender specified, let the service handle it (it will use its default)

            // Set body (prefer HTML if both are provided)
            if (!string.IsNullOrWhiteSpace(message.HtmlBody)) {
                builder.SetHtmlBody(message.HtmlBody);
                if (!string.IsNullOrWhiteSpace(message.TextBody))
                    builder.SetTextBody(message.TextBody);
            }
            else if (!string.IsNullOrWhiteSpace(message.TextBody))
                builder.SetTextBody(message.TextBody);

            // Add CC and BCC recipients
            foreach (var cc in message.CcAddresses)
                builder.AddCc(cc);

            foreach (var bcc in message.BccAddresses)
                builder.AddBcc(bcc);

            yield return builder;
        }
    }

    /// <summary>Creates a new instance of BulkEmailBuilder.</summary>
    /// <returns>A new BulkEmailBuilder instance.</returns>
    public static BulkEmailRequestBuilder New() => new();

    public override string ToString() => $"BulkEmail: {_messages.Count} messages, DefaultFrom={_defaultFromAddress ?? "(not set)"}";

    private class MessageEntry
    {
        public string To { get; set; } = null!;

        public string Subject { get; set; } = null!;

        public string? TextBody { get; set; }

        public string? HtmlBody { get; set; }

        public string? FromAddress { get; set; }

        public string? FromName { get; set; }

        public List<string> CcAddresses { get; set; } = [];

        public List<string> BccAddresses { get; set; } = [];
    }
}