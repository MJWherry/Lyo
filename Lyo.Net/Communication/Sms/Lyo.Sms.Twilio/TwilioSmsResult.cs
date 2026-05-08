using System.Globalization;
using Lyo.Common.Extensions;
using Lyo.Result;
using Lyo.Sms.Models;
using Twilio.Rest.Api.V2010.Account;

namespace Lyo.Sms.Twilio;

/// <summary>Result of a Twilio SMS/MMS operation with Twilio-specific properties.</summary>
public sealed record TwilioSmsResult : Result<SmsRequest>
{
    /// <summary>Gets the Twilio message SID.</summary>
    public string? MessageId { get; init; }

    /// <summary>Gets the Twilio delivery status for the message.</summary>
    public string? Status { get; init; }

    /// <summary>Gets the timestamp when the message was created.</summary>
    public DateTime? DateCreated { get; init; }

    /// <summary>Gets the timestamp when the message was sent.</summary>
    public DateTime? DateSent { get; init; }

    /// <summary>Gets the timestamp when the message was last updated.</summary>
    public DateTime? DateUpdated { get; init; }

    /// <summary>Gets the number of SMS segments used to send the message.</summary>
    public int? NumSegments { get; init; }

    /// <summary>Gets the Twilio account SID associated with this message.</summary>
    public string? AccountSid { get; init; }

    /// <summary>Gets the message price reported by Twilio.</summary>
    public decimal? Price { get; init; }

    /// <summary>Gets the currency unit for <see cref="Price" />.</summary>
    public string? PriceUnit { get; init; }

    /// <summary>Gets the provider-specific Twilio error code, when available.</summary>
    public int? TwilioErrorCode { get; init; }

    /// <summary>Gets or sets the message direction.</summary>
    public Direction Direction { get; set; }

    private TwilioSmsResult(bool isSuccess, SmsRequest? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful Twilio SMS result from a Twilio message resource.</summary>
    /// <param name="messageResource">The Twilio message resource returned by the API.</param>
    /// <param name="request">The SMS request represented by the Twilio message.</param>
    /// <returns>A successful <see cref="TwilioSmsResult" /> instance.</returns>
    public static TwilioSmsResult FromMessageResource(MessageResource messageResource, SmsRequest request)
        => new(true, request) {
            MessageId = messageResource.Sid,
            Status = messageResource.Status?.ToString(),
            DateCreated = messageResource.DateCreated,
            DateSent = messageResource.DateSent,
            DateUpdated = messageResource.DateUpdated,
            NumSegments = TryParseInt(messageResource.NumSegments),
            AccountSid = messageResource.AccountSid,
            Price = TryParseDecimal(messageResource.Price),
            PriceUnit = messageResource.PriceUnit,
            Direction = MapDirection(messageResource.Direction)
        };

    /// <summary>Creates a failed Twilio SMS result from an exception.</summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="request">The SMS request associated with the failure.</param>
    /// <param name="accountSid">Optional Twilio account SID associated with the failure.</param>
    /// <param name="twilioErrorCode">Optional provider-specific Twilio error code.</param>
    /// <returns>A failed <see cref="TwilioSmsResult" /> instance.</returns>
    public static TwilioSmsResult FromException(Exception exception, SmsRequest request, string? accountSid = null, int? twilioErrorCode = null)
    {
        var error = Error.FromException(exception);
        return new(false, request, [error]) { AccountSid = accountSid, TwilioErrorCode = twilioErrorCode };
    }

    /// <summary>Creates a Twilio SMS result from persisted log data.</summary>
    public static TwilioSmsResult FromLog(
        SmsRequest request,
        bool isSuccess,
        string? messageId = null,
        string? status = null,
        DateTime? dateCreated = null,
        DateTime? dateSent = null,
        DateTime? dateUpdated = null,
        int? numSegments = null,
        string? accountSid = null,
        decimal? price = null,
        string? priceUnit = null,
        int? twilioErrorCode = null,
        string? errorMessage = null,
        Direction? direction = null)
    {
        var dir = direction ?? Direction.OutboundApi;
        if (isSuccess) {
            return new(true, request) {
                MessageId = messageId,
                Status = status,
                DateCreated = dateCreated,
                DateSent = dateSent,
                DateUpdated = dateUpdated,
                NumSegments = numSegments,
                AccountSid = accountSid,
                Price = price,
                PriceUnit = priceUnit,
                Direction = dir
            };
        }

        return new(false, request, [new(errorMessage ?? "Unknown error", "SMS_SEND_FAILED")]) { TwilioErrorCode = twilioErrorCode, AccountSid = accountSid, Direction = dir };
    }

    /// <summary>Creates a failed Twilio SMS result with a custom error message.</summary>
    public static TwilioSmsResult FromError(
        string errorMessage,
        string errorCode,
        SmsRequest request,
        Exception? exception = null,
        string? accountSid = null,
        int? twilioErrorCode = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, request, [error]) { AccountSid = accountSid, TwilioErrorCode = twilioErrorCode };
    }

    /// <summary>Maps Twilio direction values to internal direction values.</summary>
    /// <param name="twilioDirection">The Twilio message direction value.</param>
    /// <returns>The mapped internal <see cref="Direction" /> value.</returns>
    private static Direction MapDirection(MessageResource.DirectionEnum? twilioDirection)
    {
        if (twilioDirection == null)
            return Direction.Unknown;

        var s = twilioDirection.ToString().ToLower();
        return s switch {
            "inbound" => Direction.Inbound,
            "outbound-api" => Direction.OutboundApi,
            "outbound-call" => Direction.OutboundCall,
            "outbound-reply" => Direction.OutboundReply,
            var _ => Direction.Unknown
        };
    }

    /// <summary>Attempts to parse a nullable integer from a string value.</summary>
    /// <param name="value">The value to parse.</param>
    /// <returns>The parsed integer, or <see langword="null" /> when parsing fails.</returns>
    private static int? TryParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (int.TryParse(value, out var result))
            return result;

        return null;
    }

    /// <summary>Attempts to parse a nullable decimal from a string value.</summary>
    /// <param name="value">The value to parse.</param>
    /// <returns>The parsed decimal, or <see langword="null" /> when parsing fails.</returns>
    private static decimal? TryParseDecimal(string? value)
    {
        if (value.IsNullOrWhitespace())
            return null;

        // Remove currency symbols and whitespace
        var cleaned = value.Trim().TrimStart('$', '€', '£', '¥');
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        return null;
    }
}