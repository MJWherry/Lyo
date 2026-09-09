using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Sms.Models;

namespace Lyo.Sms.Builders;

/// <summary>Fluent helper that builds SMS messages and checks/normalizes fields.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SmsMessageBuilder
{
    private readonly SmsRequest _request = new();

    /// <summary>Assigns the recipient number.</summary>
    /// <param name="to">Recipient in E.164 or US format.</param>
    /// <returns>This builder so further calls can chain.</returns>
    /// <exception cref="ArgumentException">Thrown when the phone number is null or empty.</exception>
    /// <exception cref="InvalidFormatException">Thrown when the phone number format is invalid.</exception>
    public SmsMessageBuilder SetTo(string to)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(to);
        if (!PhoneNumber.IsValid(to))
            throw new InvalidFormatException("To phone number is not in a valid format.", nameof(to), to, PhoneNumber.ValidFormats);

        // Convert the number to E.164
        _request.To = PhoneNumber.Normalize(to);
        return this;
    }

    /// <summary>Assigns the sender number.</summary>
    /// <param name="from">Sender in E.164 or US format.</param>
    /// <returns>This builder so further calls can chain.</returns>
    /// <exception cref="ArgumentException">Thrown when the phone number is null or empty.</exception>
    /// <exception cref="InvalidFormatException">Thrown when the phone number format is invalid.</exception>
    public SmsMessageBuilder SetFrom(string from)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(from);
        if (!PhoneNumber.IsValid(from))
            throw new InvalidFormatException("From phone number is not in a valid format.", nameof(from), from, PhoneNumber.ValidFormats);

        // Convert the sender number to E.164
        _request.From = PhoneNumber.Normalize(from);
        return this;
    }

    /// <summary>Assigns the message text.</summary>
    /// <param name="body">Body text (at most 1600 characters). May be null or empty when media is attached.</param>
    /// <returns>This builder so further calls can chain.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when body length exceeds 1600 characters.</exception>
    public SmsMessageBuilder SetBody(string? body)
    {
        if (body != null && body.Length > 1600) {
            // Cap is 1600 characters (ten 160-char segments)
            // Twilio splits longer texts; we still flag extremely long ones
            throw new ArgumentOutsideRangeException(nameof(body), body.Length, 0, 1600, $"Message body exceeds maximum length of 1600 characters. Current length: {body.Length}");
        }

        _request.Body = body;
        return this;
    }

    /// <summary>Adds text onto the current body.</summary>
    /// <param name="text">Text to append.</param>
    /// <returns>This builder so further calls can chain.</returns>
    /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when appending would exceed the 1600 character limit.</exception>
    public SmsMessageBuilder AppendBody(string text)
    {
        var newBody = (_request.Body ?? string.Empty) + text;

        // Enforce the length cap
        ArgumentHelpers.ThrowIfNotInRange(newBody.Length, -1, 1600, nameof(text));
        _request.Body = newBody;
        return this;
    }

    /// <summary>Attaches a media URL.</summary>
    /// <param name="url">Public URL of the media file.</param>
    /// <returns>This builder so further calls can chain.</returns>
    /// <exception cref="ArgumentException">Thrown when URL is null, empty, or invalid.</exception>
    /// <exception cref="InvalidFormatException">Thrown when URL format is invalid, not absolute, or not a web URL.</exception>
    public SmsMessageBuilder AddAttachment(string url)
    {
        var uri = UriHelpers.GetValidWebUri(url);
        _request.MediaUrls.Add(uri);
        return this;
    }

    /// <summary>Resets every message field.</summary>
    /// <returns>This builder so further calls can chain.</returns>
    public SmsMessageBuilder Clear()
    {
        _request.To = null!;
        _request.From = null;
        _request.Body = null!;
        _request.MediaUrls.Clear();
        return this;
    }

    /// <summary>Validates fields and produces an SmsMessage.</summary>
    /// <returns>A validated SmsMessage.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required fields are missing or message has neither body nor media.</exception>
    /// <exception cref="InvalidFormatException">Thrown when phone number formats are invalid.</exception>
    public SmsRequest Build()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_request.To, nameof(_request.To));
        if (!PhoneNumber.IsValid(_request.To!))
            throw new InvalidFormatException("To phone number is not in a valid format.", nameof(_request.To), _request.To, PhoneNumber.ValidFormats);

        // Require a body or at least one media URL
        var hasBody = !string.IsNullOrWhiteSpace(_request.Body);
        var hasMedia = _request.MediaUrls.Count > 0;
        if (!hasBody && !hasMedia)
            OperationHelpers.ThrowIf(true, "Message must have either a body or at least one media attachment.");

        // Sender is optional; validate it when present
        if (!string.IsNullOrWhiteSpace(_request.From) && !PhoneNumber.IsValid(_request.From!))
            throw new InvalidFormatException("From phone number is not in a valid format.", nameof(_request.From), _request.From, PhoneNumber.ValidFormats);

        return new() {
            From = _request.From,
            To = _request.To,
            Body = _request.Body,
            MediaUrls = [.. _request.MediaUrls]
        };
    }

    /// <summary>Starts a new SmsMessageBuilder.</summary>
    /// <returns>A fresh SmsMessageBuilder.</returns>
    public static SmsMessageBuilder New() => new();

    /// <summary>Diagnostic snapshot of the current SMS builder.</summary>
    /// <returns>Text with sender, recipient, body length, and attachment count.</returns>
    public override string ToString()
        => $"SMS: To={_request.To ?? "(not set)"}, From={_request.From ?? "(not set)"}, Body length={_request.Body?.Length ?? 0}, Media={_request.MediaUrls.Count}";
}