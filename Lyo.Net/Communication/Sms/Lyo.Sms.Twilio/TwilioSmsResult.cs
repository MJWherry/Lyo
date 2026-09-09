using System.Globalization;
using Lyo.Common.Core.Extensions;
using Lyo.Result;
using Lyo.Sms.Models;
using Twilio.Rest.Api.V2010.Account;

namespace Lyo.Sms.Twilio;

/// <summary>Outcome of a Twilio SMS or MMS send, including Twilio-only fields.</summary>
public sealed record TwilioSmsResult : Result<SmsRequest>
{
    /// <summary>Twilio message SID.</summary>
    public string? MessageId { get; init; }

    /// <summary>Twilio delivery status.</summary>
    public string? Status { get; init; }

    /// <summary>When the message was created.</summary>
    public DateTime? DateCreated { get; init; }

    /// <summary>When the message was sent.</summary>
    public DateTime? DateSent { get; init; }

    /// <summary>When the message was last updated.</summary>
    public DateTime? DateUpdated { get; init; }

    /// <summary>How many SMS segments were used.</summary>
    public int? NumSegments { get; init; }

    /// <summary>Twilio account SID for this message.</summary>
    public string? AccountSid { get; init; }

    /// <summary>Price Twilio reported for the message.</summary>
    public decimal? Price { get; init; }

    /// <summary>Currency of <see cref="Price" />.</summary>
    public string? PriceUnit { get; init; }

    /// <summary>Twilio error code, if the provider supplied one.</summary>
    public int? TwilioErrorCode { get; init; }

    /// <summary>Whether the message is inbound or outbound.</summary>
    public Direction Direction { get; set; }

    private TwilioSmsResult(bool isSuccess, SmsRequest? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Builds a successful outcome from a Twilio message resource.</summary>
    /// <param name="messageResource">Resource returned by the Twilio API.</param>
    /// <param name="request">SMS request that corresponds to that message.</param>
    /// <returns>A successful <see cref="TwilioSmsResult" />.</returns>
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

    /// <summary>Builds a failed outcome from an exception.</summary>
    /// <param name="exception">Error that stopped the send.</param>
    /// <param name="request">Request that failed.</param>
    /// <param name="accountSid">Optional Twilio account SID for the failure.</param>
    /// <param name="twilioErrorCode">Optional Twilio error code.</param>
    /// <returns>A failed <see cref="TwilioSmsResult" />.</returns>
    public static TwilioSmsResult FromException(Exception exception, SmsRequest request, string? accountSid = null, int? twilioErrorCode = null)
    {
        var error = Error.FromException(exception);
        return new(false, request, [error]) { AccountSid = accountSid, TwilioErrorCode = twilioErrorCode };
    }

    /// <summary>Rebuilds a Twilio SMS result from stored log data.</summary>
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

    /// <summary>Builds a failed outcome with a supplied message.</summary>
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

    /// <summary>Maps a Twilio direction string to the internal <see cref="Direction" />.</summary>
    /// <param name="twilioDirection">Twilio direction value.</param>
    /// <returns>The matching internal <see cref="Direction" />.</returns>
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

    /// <summary>Tries to parse a nullable integer from a string.</summary>
    /// <param name="value">Text to parse.</param>
    /// <returns>The integer, or <see langword="null" /> if parsing fails.</returns>
    private static int? TryParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (int.TryParse(value, out var result))
            return result;

        return null;
    }

    /// <summary>Tries to parse a nullable decimal from a string.</summary>
    /// <param name="value">Text to parse.</param>
    /// <returns>The decimal, or <see langword="null" /> if parsing fails.</returns>
    private static decimal? TryParseDecimal(string? value)
    {
        if (value.IsNullOrWhitespace())
            return null;

        // Strip currency symbols and whitespace
        var cleaned = value.Trim().TrimStart('$', '€', '£', '¥');
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        return null;
    }
}