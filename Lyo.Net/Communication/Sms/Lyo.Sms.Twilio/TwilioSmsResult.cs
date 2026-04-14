using System.Globalization;
using Lyo.Common;
using Lyo.Sms.Models;
using Twilio.Rest.Api.V2010.Account;

namespace Lyo.Sms.Twilio;

/// <summary>Result of a Twilio SMS/MMS operation with Twilio-specific properties.</summary>
public sealed record TwilioSmsResult : Result<SmsRequest>
{
    public string? MessageId { get; init; }

    public string? Status { get; init; }

    public DateTime? DateCreated { get; init; }

    public DateTime? DateSent { get; init; }

    public DateTime? DateUpdated { get; init; }

    public int? NumSegments { get; init; }

    public string? AccountSid { get; init; }

    public decimal? Price { get; init; }

    public string? PriceUnit { get; init; }

    public int? TwilioErrorCode { get; init; }

    public Direction Direction { get; set; }

    private TwilioSmsResult(bool isSuccess, SmsRequest? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful TwilioSmsResult from a Twilio MessageResource.</summary>
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

    /// <summary>Creates a failed TwilioSmsResult from an exception.</summary>
    public static TwilioSmsResult FromException(Exception exception, SmsRequest request, string? accountSid = null, int? twilioErrorCode = null)
    {
        var error = Error.FromException(exception);
        return new(false, request, [error]) { AccountSid = accountSid, TwilioErrorCode = twilioErrorCode };
    }

    /// <summary>Creates a TwilioSmsResult from persisted log data (e.g. TwilioSmsLogEntity).</summary>
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

    /// <summary>Creates a failed TwilioSmsResult with a custom error message.</summary>
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

    private static int? TryParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (int.TryParse(value, out var result))
            return result;

        return null;
    }

    private static decimal? TryParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Remove currency symbols and whitespace
        var cleaned = value.Trim().TrimStart('$', '€', '£', '¥');
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        return null;
    }
}