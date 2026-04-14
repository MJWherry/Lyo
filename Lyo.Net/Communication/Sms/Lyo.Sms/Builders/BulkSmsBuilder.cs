using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Sms.Models;

namespace Lyo.Sms.Builders;

/// <summary>Builder class for constructing bulk SMS messages with a default sender.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class BulkSmsBuilder
{
    private readonly List<MessageEntry> _messages = [];

    private string? _defaultFrom;

    private int? _maxLimit;

    /// <summary>Gets the number of messages in this bulk send.</summary>
    public int Count => _messages.Count;

    /// <summary>Sets the maximum number of messages allowed in this bulk send.</summary>
    /// <param name="maxLimit">The maximum number of messages (must be greater than 0).</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when maxLimit is outside the range [1, int.MaxValue].</exception>
    public BulkSmsBuilder SetMaxLimit(int maxLimit)
    {
        ArgumentHelpers.ThrowIfNullOrNotInRange(maxLimit, 1, int.MaxValue, nameof(maxLimit));
        _maxLimit = maxLimit;
        return this;
    }

    /// <summary>Sets the default sender phone number for all messages in this bulk send.</summary>
    /// <param name="from">The default sender phone number (E.164 format or US format).</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when the phone number is null, empty, or invalid.</exception>
    public BulkSmsBuilder SetDefaultFrom(string from)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(from, nameof(from));
        if (!PhoneNumber.Normalize(from)?.StartsWith("+") == true)
            throw new InvalidFormatException("From phone number is not in a valid format.", nameof(from), from, PhoneNumber.ValidFormats);

        _defaultFrom = PhoneNumber.Normalize(from);
        return this;
    }

    /// <summary>Adds a message to the bulk send.</summary>
    /// <param name="to">The recipient phone number (E.164 format or US format).</param>
    /// <param name="body">The message body (max 1600 characters). Can be null or empty if media attachments are provided.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public BulkSmsBuilder Add(string to, string? body)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(to, nameof(to));
        if (body != null && body.Length > 1600)
            throw new ArgumentOutsideRangeException(nameof(body), body.Length, 0, 1600, $"Message body exceeds maximum length of 1600 characters. Current length: {body.Length}");

        if (_maxLimit.HasValue && _messages.Count >= _maxLimit.Value) {
            throw new ArgumentOutsideRangeException(
                nameof(to), _messages.Count + 1, 1, _maxLimit.Value, $"Cannot add more messages. Maximum limit of {_maxLimit.Value} messages has been reached.");
        }

        _messages.Add(new() { To = to, Body = body, From = null });
        return this;
    }

    /// <summary>Adds a message to the bulk send with a specific sender (overrides default).</summary>
    /// <param name="to">The recipient phone number (E.164 format or US format).</param>
    /// <param name="body">The message body (max 1600 characters). Can be null or empty if media attachments are provided.</param>
    /// <param name="from">The sender phone number (optional, uses default if not provided).</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public BulkSmsBuilder Add(string to, string? body, string? from)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(to, nameof(to));
        if (body != null && body.Length > 1600)
            throw new ArgumentOutsideRangeException(nameof(body), body.Length, 0, 1600, $"Message body exceeds maximum length of 1600 characters. Current length: {body.Length}");

        if (_maxLimit.HasValue && _messages.Count >= _maxLimit.Value) {
            throw new ArgumentOutsideRangeException(
                nameof(to), _messages.Count + 1, 1, _maxLimit.Value, $"Cannot add more messages. Maximum limit of {_maxLimit.Value} messages has been reached.");
        }

        string? normalizedFrom = null;
        if (!string.IsNullOrWhiteSpace(from)) {
            normalizedFrom = PhoneNumber.Normalize(from);
            if (normalizedFrom == null || !normalizedFrom.StartsWith("+"))
                throw new InvalidFormatException("From phone number is not in a valid format.", nameof(from), from, PhoneNumber.ValidFormats);
        }

        _messages.Add(new() { To = to, Body = body, From = normalizedFrom });
        return this;
    }

    /// <summary>Adds a media attachment URL to the last message added.</summary>
    /// <param name="url">The publicly accessible URL of the media file.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no messages have been added yet.</exception>
    /// <exception cref="ArgumentException">Thrown when URL is null, empty, or invalid.</exception>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when maximum media count (10) is exceeded.</exception>
    /// <exception cref="InvalidFormatException">Thrown when URL format is invalid or not absolute.</exception>
    public BulkSmsBuilder AddAttachment(string url)
    {
        if (_messages.Count == 0)
            OperationHelpers.ThrowIf(true, "Cannot add attachment: No messages have been added yet. Call Add() first.");

        var lastMessage = _messages[_messages.Count - 1];
        return AddAttachmentToMessage(_messages.Count - 1, url);
    }

    /// <summary>Adds a media attachment URL to a specific message by index (0-based).</summary>
    /// <param name="messageIndex">The zero-based index of the message to add the attachment to.</param>
    /// <param name="url">The publicly accessible URL of the media file.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when messageIndex is out of range.</exception>
    /// <exception cref="ArgumentException">Thrown when URL is null, empty, or invalid.</exception>
    /// <exception cref="InvalidFormatException">Thrown when URL format is invalid or not absolute.</exception>
    public BulkSmsBuilder AddAttachmentToMessage(int messageIndex, string url)
    {
        if (messageIndex < 0 || messageIndex >= _messages.Count) {
            throw new ArgumentOutsideRangeException(
                nameof(messageIndex), messageIndex, 0, _messages.Count - 1, $"Message index {messageIndex} is out of range. Valid range: 0 to {_messages.Count - 1}.");
        }

        var uri = UriHelpers.GetValidWebUri(url, nameof(url));
        var message = _messages[messageIndex];
        message.MediaUrls.Add(uri);
        return this;
    }

    /// <summary>Clears all messages and the default sender.</summary>
    /// <returns>The builder instance for method chaining.</returns>
    public BulkSmsBuilder Clear()
    {
        _messages.Clear();
        _defaultFrom = null;
        _maxLimit = null;
        return this;
    }

    /// <summary>Builds the collection of SMS message builders from the bulk builder.</summary>
    /// <returns>Collection of SmsMessageBuilder instances.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no messages are added or default sender is not set and messages don't have individual senders.</exception>
    public IEnumerable<SmsMessageBuilder> Build()
    {
        if (_messages.Count == 0)
            OperationHelpers.ThrowIf(true, "No messages have been added to the bulk send");

        foreach (var message in _messages) {
            var builder = SmsMessageBuilder.New().SetTo(message.To).SetBody(message.Body);

            // Use message-specific sender if provided, otherwise use default
            if (!string.IsNullOrWhiteSpace(message.From))
                builder.SetFrom(message.From!);
            else if (!string.IsNullOrWhiteSpace(_defaultFrom))
                builder.SetFrom(_defaultFrom!);
            // If no sender specified, let the service handle it (it will use its default)

            // Add media attachments
            foreach (var mediaUrl in message.MediaUrls)
                builder.AddAttachment(mediaUrl.ToString());

            yield return builder;
        }
    }

    /// <summary>Creates a new instance of BulkSmsBuilder.</summary>
    /// <returns>A new BulkSmsBuilder instance.</returns>
    public static BulkSmsBuilder New() => new();

    public override string ToString() => $"BulkSMS: {_messages.Count} messages, DefaultFrom={_defaultFrom ?? "(not set)"}";

    [DebuggerDisplay("{ToString(),nq}")]
    private class MessageEntry
    {
        public string To { get; set; } = null!;

        public string? Body { get; set; }

        public string? From { get; set; }

        public List<Uri> MediaUrls { get; } = new();

        public override string ToString() => $"To={To}, From={From ?? "(default)"}, BodyLength={Body?.Length ?? 0}, MediaCount={MediaUrls.Count}";
    }
}