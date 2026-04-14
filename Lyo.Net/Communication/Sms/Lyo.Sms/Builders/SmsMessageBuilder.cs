using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Sms.Models;

namespace Lyo.Sms.Builders;

/// <summary>Builder class for constructing SMS messages with validation and normalization.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SmsMessageBuilder
{
    private readonly SmsRequest _request = new();

    /// <summary>Sets the recipient phone number.</summary>
    /// <param name="to">The recipient phone number in E.164 format or US format.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when the phone number is null or empty.</exception>
    /// <exception cref="InvalidFormatException">Thrown when the phone number format is invalid.</exception>
    public SmsMessageBuilder SetTo(string to)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(to, nameof(to));
        if (!PhoneNumber.IsValid(to))
            throw new InvalidFormatException("To phone number is not in a valid format.", nameof(to), to, PhoneNumber.ValidFormats);

        // Normalize to E.164 format
        _request.To = PhoneNumber.Normalize(to);
        return this;
    }

    /// <summary>Sets the sender phone number.</summary>
    /// <param name="from">The sender phone number in E.164 format or US format.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when the phone number is null or empty.</exception>
    /// <exception cref="InvalidFormatException">Thrown when the phone number format is invalid.</exception>
    public SmsMessageBuilder SetFrom(string from)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(from, nameof(from));
        if (!PhoneNumber.IsValid(from))
            throw new InvalidFormatException("From phone number is not in a valid format.", nameof(from), from, PhoneNumber.ValidFormats);

        // Normalize to E.164 format
        _request.From = PhoneNumber.Normalize(from);
        return this;
    }

    /// <summary>Sets the message body.</summary>
    /// <param name="body">The message body text (max 1600 characters). Can be null or empty if media attachments are provided.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when body length exceeds 1600 characters.</exception>
    public SmsMessageBuilder SetBody(string? body)
    {
        if (body != null && body.Length > 1600) {
            // SMS has a 1600 character limit (10 segments of 160 chars each)
            // Twilio will automatically split longer messages, but we warn about very long messages
            throw new ArgumentOutsideRangeException(nameof(body), body.Length, 0, 1600, $"Message body exceeds maximum length of 1600 characters. Current length: {body.Length}");
        }

        _request.Body = body;
        return this;
    }

    /// <summary>Appends text to the existing message body.</summary>
    /// <param name="text">The text to append.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when appending would exceed the 1600 character limit.</exception>
    public SmsMessageBuilder AppendBody(string text)
    {
        var newBody = (_request.Body ?? string.Empty) + text;

        // Check length limit
        ArgumentHelpers.ThrowIfNotInRange(newBody.Length, -1, 1600, nameof(text));
        _request.Body = newBody;
        return this;
    }

    /// <summary>Adds a media attachment URL to the message.</summary>
    /// <param name="url">The publicly accessible URL of the media file.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when URL is null, empty, or invalid.</exception>
    /// <exception cref="InvalidFormatException">Thrown when URL format is invalid, not absolute, or not a web URL.</exception>
    public SmsMessageBuilder AddAttachment(string url)
    {
        var uri = UriHelpers.GetValidWebUri(url, nameof(url));
        _request.MediaUrls.Add(uri);
        return this;
    }

    /// <summary>Clears all message properties.</summary>
    /// <returns>The builder instance for method chaining.</returns>
    public SmsMessageBuilder Clear()
    {
        _request.To = null!;
        _request.From = null;
        _request.Body = null!;
        _request.MediaUrls.Clear();
        return this;
    }

    /// <summary>Builds and validates the SMS message.</summary>
    /// <returns>A validated SmsMessage instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required fields are missing or message has neither body nor media.</exception>
    /// <exception cref="InvalidFormatException">Thrown when phone number formats are invalid.</exception>
    public SmsRequest Build()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_request.To, nameof(_request.To));
        if (!PhoneNumber.IsValid(_request.To!))
            throw new InvalidFormatException("To phone number is not in a valid format.", nameof(_request.To), _request.To, PhoneNumber.ValidFormats);

        // Message must have either a body or at least one media attachment
        var hasBody = !string.IsNullOrWhiteSpace(_request.Body);
        var hasMedia = _request.MediaUrls.Count > 0;
        if (!hasBody && !hasMedia)
            OperationHelpers.ThrowIf(true, "Message must have either a body or at least one media attachment.");

        // From is optional, but if provided, validate it
        if (!string.IsNullOrWhiteSpace(_request.From) && !PhoneNumber.IsValid(_request.From!))
            throw new InvalidFormatException("From phone number is not in a valid format.", nameof(_request.From), _request.From, PhoneNumber.ValidFormats);

        return new() {
            From = _request.From,
            To = _request.To,
            Body = _request.Body,
            MediaUrls = [.._request.MediaUrls]
        };
    }

    /// <summary>Creates a new instance of SmsMessageBuilder.</summary>
    /// <returns>A new SmsMessageBuilder instance.</returns>
    public static SmsMessageBuilder New() => new();

    public override string ToString()
        => $"SMS: To={_request.To ?? "(not set)"}, From={_request.From ?? "(not set)"}, Body length={_request.Body?.Length ?? 0}, Media={_request.MediaUrls.Count}";
}