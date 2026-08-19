using System.Text.Json;
using MudBlazor;

namespace Lyo.Sms.Web.Components;

/// <summary>MudBlazor chip colors for Twilio SMS log status, direction, and success.</summary>
public static class SmsColorHelper
{
    /// <summary>Chip color for a Twilio/log status string (delivered, failed, received, queued, …).</summary>
    public static Color ForStatus(string? status)
    {
        var key = Normalize(status);
        if (key.Length == 0)
            return Color.Default;

        return key switch {
            "delivered" or "sent" or "received" or "read" => Color.Success,
            "queued" or "accepted" or "scheduled" or "sending" or "receiving" => Color.Info,
            "failed" or "undelivered" or "canceled" or "cancelled" => Color.Error,
            "partially_delivered" or "partially delivered" => Color.Warning,
            var _ => Color.Default
        };
    }

    /// <summary>Chip color for inbound vs outbound (including Twilio outbound-api / call / reply).</summary>
    public static Color ForDirection(string? direction)
        => IsInbound(direction) ? Color.Info : IsOutbound(direction) ? Color.Primary : Color.Default;

    /// <summary>Chip color for send/receive success.</summary>
    public static Color ForSuccess(bool? success)
        => success switch {
            true => Color.Success,
            false => Color.Error,
            var _ => Color.Default
        };

    /// <summary>True when the stored direction is inbound (enum name, Twilio string, or numeric 1).</summary>
    public static bool IsInbound(string? direction)
    {
        var key = Normalize(direction);
        return key is "inbound" or "1" || key == ((int)Lyo.Sms.Models.Direction.Inbound).ToString();
    }

    /// <summary>True when the stored direction is any outbound variant.</summary>
    public static bool IsOutbound(string? direction)
    {
        var key = Normalize(direction);
        return key is "outbound" or "outbound-api" or "outboundapi" or "outbound-call" or "outboundcall" or "outbound-reply" or "outboundreply" or "0"
               || key == ((int)Lyo.Sms.Models.Direction.OutboundApi).ToString();
    }

    /// <summary>Parses a projected bool (CLR, JSON, or string). Null/unknown returns null.</summary>
    public static bool? ToBool(object? value)
        => value switch {
            null => null,
            bool b => b,
            JsonElement { ValueKind: JsonValueKind.True } => true,
            JsonElement { ValueKind: JsonValueKind.False } => false,
            JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => null,
            JsonElement { ValueKind: JsonValueKind.String } el => ToBool(el.GetString()),
            string s when string.IsNullOrWhiteSpace(s) => null,
            string s when bool.TryParse(s, out var parsed) => parsed,
            var _ => null
        };

    private static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? "" : value.Trim().Replace('_', '-').ToLowerInvariant();
}
