using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Sms.Models;

namespace Lyo.Sms.Builders;

/// <summary>Fluent helper that assembles a batch of SMS messages with a shared default sender.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class BulkSmsBuilder
{
    private readonly List<MessageEntry> _messages = [];

    private string? _defaultFrom;

    private int? _maxLimit;

    /// <summary>How many messages are queued in this batch.</summary>
    public int Count => _messages.Count;

    /// <summary>Caps how many messages this batch may hold.</summary>
    /// <param name="maxLimit">Upper bound (must be greater than 0).</param>
    /// <returns>This builder so further calls can chain.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when maxLimit is outside the range [1, int.MaxValue].</exception>
    public BulkSmsBuilder SetMaxLimit(int maxLimit)
    {
        ArgumentHelpers.ThrowIfNullOrNotInRange(maxLimit, 1, int.MaxValue);
        _maxLimit = maxLimit;
        return this;
    }

    /// <summary>Assigns the default sender used when a message omits its own.</summary>
    /// <param name="from">Default sender (E.164 or US format).</param>
    /// <returns>This builder so further calls can chain.</returns>
    /// <exception cref="ArgumentException">Thrown when the phone number is null, empty, or invalid.</exception>
    public BulkSmsBuilder SetDefaultFrom(string from)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(from);
        if (!PhoneNumber.Normalize(from)?.StartsWith("+") == true)
            throw new InvalidFormatException("From phone number is not in a valid format.", nameof(from), from, PhoneNumber.ValidFormats);

        _defaultFrom = PhoneNumber.Normalize(from);
        return this;
    }

    /// <summary>Queues a message in the batch.</summary>
    /// <param name="to">Recipient (E.164 or US format).</param>
    /// <param name="body">Body (at most 1600 characters). May be null or empty when media is attached.</param>
    /// <returns>This builder so further calls can chain.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public BulkSmsBuilder Add(string to, string? body)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(to);
        if (body != null && body.Length > 1600)
            throw new ArgumentOutsideRangeException(nameof(body), body.Length, 0, 1600, $"Message body exceeds maximum length of 1600 characters. Current length: {body.Length}");

        if (_maxLimit.HasValue && _messages.Count >= _maxLimit.Value) {
            throw new ArgumentOutsideRangeException(
                nameof(to), _messages.Count + 1, 1, _maxLimit.Value, $"Cannot add more messages. Maximum limit of {_maxLimit.Value} messages has been reached.");
        }

        _messages.Add(new() { To = to, Body = body, From = null });
        return this;
    }

    /// <summary>Queues a message and optionally overrides the default sender.</summary>
    /// <param name="to">Recipient (E.164 or US format).</param>
    /// <param name="body">Body (at most 1600 characters). May be null or empty when media is attached.</param>
    /// <param name="from">Sender (optional; falls back to the default).</param>
    /// <returns>This builder so further calls can chain.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public BulkSmsBuilder Add(string to, string? body, string? from)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(to);
        if (body != null && body.Length > 1600)
            throw new ArgumentOutsideRangeException(nameof(body), body.Length, 0, 1600, $"Message body exceeds maximum length of 1600 characters. Current length: {body.Length}");

        if (_maxLimit.HasValue && _messages.Count >= _maxLimit.Value) {
            throw new ArgumentOutsideRangeException(
                nameof(to), _messages.Count, 1, _maxLimit.Value, $"Cannot add more messages. Maximum limit of {_maxLimit.Value} messages has been reached.");
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

    /// <summary>Attaches a media URL to the most recently added message.</summary>
    /// <param name="url">Public URL of the media file.</param>
    /// <returns>This builder so further calls can chain.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no messages have been added yet.</exception>
    /// <exception cref="ArgumentException">Thrown when URL is null, empty, or invalid.</exception>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when maximum media count (10) is exceeded.</exception>
    /// <exception cref="InvalidFormatException">Thrown when URL format is invalid or not absolute.</exception>
    public BulkSmsBuilder AddAttachment(string url)
    {
        if (_messages.Count == 0)
            OperationHelpers.ThrowIf(true, "Cannot add attachment: No messages have been added yet. Call Add() first.");

        return AddAttachmentToMessage(_messages.Count - 1, url);
    }

    /// <summary>Attaches a media URL to the message at a zero-based index.</summary>
    /// <param name="messageIndex">Zero-based index of the target message.</param>
    /// <param name="url">Public URL of the media file.</param>
    /// <returns>This builder so further calls can chain.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when messageIndex is out of range.</exception>
    /// <exception cref="ArgumentException">Thrown when URL is null, empty, or invalid.</exception>
    /// <exception cref="InvalidFormatException">Thrown when URL format is invalid or not absolute.</exception>
    public BulkSmsBuilder AddAttachmentToMessage(int messageIndex, string url)
    {
        ArgumentHelpers.ThrowIfNotInRange(messageIndex, 0, _messages.Count);
        var uri = UriHelpers.GetValidWebUri(url);
        var message = _messages[messageIndex];
        message.MediaUrls.Add(uri);
        return this;
    }

    /// <summary>Removes every queued message and the default sender.</summary>
    /// <returns>This builder so further calls can chain.</returns>
    public BulkSmsBuilder Clear()
    {
        _messages.Clear();
        _defaultFrom = null;
        _maxLimit = null;
        return this;
    }

    /// <summary>Produces SmsMessageBuilder instances from the queued entries.</summary>
    /// <returns>The constructed SmsMessageBuilder collection.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no messages are added or default sender is not set and messages don't have individual senders.</exception>
    public IEnumerable<SmsMessageBuilder> Build()
    {
        if (_messages.Count == 0)
            OperationHelpers.ThrowIf(true, "No messages have been added to the bulk send");

        foreach (var message in _messages) {
            var builder = SmsMessageBuilder.New().SetTo(message.To).SetBody(message.Body);

            // Prefer the per-message sender; otherwise the default
            if (!string.IsNullOrWhiteSpace(message.From))
                builder.SetFrom(message.From!);
            else if (!string.IsNullOrWhiteSpace(_defaultFrom))
                builder.SetFrom(_defaultFrom!);
            // Leave sender unset so the service applies its own default

            // Attach media URLs
            foreach (var mediaUrl in message.MediaUrls)
                builder.AddAttachment(mediaUrl.ToString());

            yield return builder;
        }
    }

    /// <summary>Starts a new BulkSmsBuilder.</summary>
    /// <returns>A fresh BulkSmsBuilder.</returns>
    public static BulkSmsBuilder New() => new();

    /// <summary>Diagnostic snapshot of the current bulk builder.</summary>
    /// <returns>Text with message count and the default sender.</returns>
    public override string ToString() => $"BulkSMS: {_messages.Count} messages, DefaultFrom={_defaultFrom ?? "(not set)"}";

    [DebuggerDisplay("{ToString(),nq}")]
    private class MessageEntry
    {
        /// <summary>Recipient number.</summary>
        public string To { get; set; } = null!;

        /// <summary>SMS body.</summary>
        public string? Body { get; set; }

        /// <summary>Sender number.</summary>
        public string? From { get; set; }

        /// <summary>Media URLs on this entry.</summary>
        public List<Uri> MediaUrls { get; } = new();

        /// <summary>Diagnostic snapshot of this queued entry.</summary>
        /// <returns>Text with recipient, sender, body length, and media count.</returns>
        public override string ToString() => $"To={To}, From={From ?? "(default)"}, BodyLength={Body?.Length ?? 0}, MediaCount={MediaUrls.Count}";
    }
}