using System.Text.Json;
using Lyo.Common.Metadata.Extensions;
using MudBlazor;
using Direction = Lyo.Sms.Models.Direction;

namespace Lyo.Sms.Web.Components;

/// <summary>MudBlazor chip colors for Twilio SMS log status, direction, and success flags.</summary>
public static class SmsColorHelper
{
    /// <summary>Chip color for a Twilio or log status (delivered, failed, received, queued, and similar).</summary>
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

    /// <summary>Chip color for inbound versus outbound (including Twilio outbound-api, call, and reply).</summary>
    public static Color ForDirection(string? direction)
        => IsInbound(direction) ? Color.Info : IsOutbound(direction) ? Color.Primary : Color.Default;

    /// <summary>Chip color for a successful send or receive.</summary>
    public static Color ForSuccess(bool? success)
        => success switch {
            true => Color.Success,
            false => Color.Error,
            var _ => Color.Default
        };

    /// <summary>Whether the stored direction is inbound (enum name, Twilio string, or numeric ordinal).</summary>
    public static bool IsInbound(string? direction) => Tokens(Direction.Inbound).Contains(Normalize(direction));

    /// <summary>Whether the stored direction is any outbound form (<c>outbound-api</c>, <c>outbound-call</c>, <c>outbound-reply</c>, or plain <c>outbound</c>).</summary>
    public static bool IsOutbound(string? direction)
    {
        var key = Normalize(direction);
        return key == "outbound" || OutboundDirections.Any(d => Tokens(d).Contains(key));
    }

    private static readonly Direction[] OutboundDirections = [Direction.OutboundApi, Direction.OutboundCall, Direction.OutboundReply];

    /// <summary>
    /// All stored spellings that mean <paramref name="direction" />: the enum name, its <c>[StringValue]</c> wire form, that form without hyphens, and the numeric ordinal.
    /// These come from the enum so a new <see cref="Direction" /> member is picked up without editing this file.
    /// </summary>
    private static string[] Tokens(Direction direction)
        => [Normalize(direction.ToString()), Normalize(direction.GetStringValue()), Normalize(direction.GetStringValue()).Replace("-", ""), ((int)direction).ToString()];

    /// <summary>Reads a projected bool (CLR, JSON, or string). Null or unknown yields null.</summary>
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
