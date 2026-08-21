using Lyo.Common.Conversion;
using Lyo.MessageQueue.RabbitMq;
using Lyo.Web.Components;
using MudBlazor;

namespace Lyo.MessageQueue.RabbitMq.Web.Components;

/// <summary>MudBlazor colors and chip specs for RabbitMQ queue/exchange flags. Rendering goes through <see cref="LyoChip" />.</summary>
public static class RabbitMqColorHelper
{
    /// <summary>Color for durable vs transient.</summary>
    public static Color ForDurable(bool durable) => durable ? Color.Success : Color.Default;

    /// <summary>Color for a broker-reported queue state (<c>running</c> is success).</summary>
    public static Color ForState(string? state)
        => string.Equals(state, "running", StringComparison.OrdinalIgnoreCase) ? Color.Success
            : string.IsNullOrWhiteSpace(state) ? Color.Default
            : Color.Warning;

    /// <summary>Color for an exchange type (direct/topic/fanout/headers).</summary>
    public static Color ForExchangeType(string? type)
        => type?.ToLowerInvariant() switch {
            "direct" => Color.Primary,
            "topic" => Color.Info,
            "fanout" => Color.Tertiary,
            "headers" => Color.Warning,
            var _ => Color.Default
        };

    /// <summary>Filled chip when <paramref name="on" /> is true, outlined otherwise.</summary>
    public static Variant ToggleVariant(bool on) => on ? Variant.Filled : Variant.Outlined;

    /// <summary><paramref name="active" /> when on, otherwise <see cref="Color.Default" />.</summary>
    public static Color ToggleColor(bool on, Color active) => on ? active : Color.Default;

    /// <summary>Durable vs transient chip from management-API additional properties.</summary>
    public static LyoChipSpec DurableChip(IReadOnlyDictionary<string, object>? properties)
        => LyoChips.FromBool(ReadBool(properties, Constants.QueueInfoProperties.Durable), "Durable", "Transient", Color.Success, Color.Default);

    /// <summary>Durable vs transient chip.</summary>
    public static LyoChipSpec DurableChip(bool durable)
        => LyoChips.FromBool(durable, "Durable", "Transient", Color.Success, Color.Default);

    /// <summary>Exclusive chip from additional properties, or null when the queue is shared.</summary>
    public static LyoChipSpec? ExclusiveChip(IReadOnlyDictionary<string, object>? properties)
        => ExclusiveChip(ReadBool(properties, Constants.QueueInfoProperties.Exclusive));

    /// <summary>Exclusive chip, or null when the queue is shared.</summary>
    public static LyoChipSpec? ExclusiveChip(bool exclusive)
        => exclusive ? LyoChips.Of("Exclusive", Color.Warning) : null;

    /// <summary>Auto-delete chip from additional properties, or null when the object is not auto-deleted.</summary>
    public static LyoChipSpec? AutoDeleteChip(IReadOnlyDictionary<string, object>? properties)
        => AutoDeleteChip(ReadBool(properties, Constants.QueueInfoProperties.AutoDelete));

    /// <summary>Auto-delete chip, or null when the object is not auto-deleted.</summary>
    public static LyoChipSpec? AutoDeleteChip(bool autoDelete)
        => autoDelete ? LyoChips.Of("Auto-delete", Color.Warning) : null;

    /// <summary>Internal exchange chip, or null when the exchange is public.</summary>
    public static LyoChipSpec? InternalChip(bool isInternal)
        => isInternal ? LyoChips.Of("Internal", Color.Default) : null;

    /// <summary>Queue state chip, or null when the broker did not report a state.</summary>
    public static LyoChipSpec? StateChip(string? state)
        => string.IsNullOrWhiteSpace(state) ? null : LyoChips.Of(state, ForState(state));

    /// <summary>Classic/quorum/etc. chip, or null when type is missing.</summary>
    public static LyoChipSpec? QueueTypeChip(string? type)
        => string.IsNullOrWhiteSpace(type) ? null : LyoChips.Of(type, Color.Info, variant: Variant.Outlined);

    /// <summary>Exchange type chip.</summary>
    public static LyoChipSpec ExchangeTypeChip(string? type)
        => LyoChips.Of(string.IsNullOrWhiteSpace(type) ? "—" : type, ForExchangeType(type));

    /// <summary>DLQ chip when <c>x-dead-letter-routing-key</c> is in additional properties.</summary>
    public static LyoChipSpec? DlqChip(IReadOnlyDictionary<string, object>? properties)
        => DeadLetterRoutingKey(properties) is null ? null : LyoChips.Of("DLQ", Color.Tertiary);

    /// <summary>Priority chip when <c>x-max-priority</c> is greater than zero.</summary>
    public static LyoChipSpec? MaxPriorityChip(IReadOnlyDictionary<string, object>? properties)
    {
        var max = MaxPriority(properties);
        return max is null or <= 0 ? null : LyoChips.Of($"Priority {max}", Color.Primary, variant: Variant.Outlined);
    }

    /// <summary>Dead-letter routing key from additional properties, or null when unset.</summary>
    public static string? DeadLetterRoutingKey(IReadOnlyDictionary<string, object>? properties)
    {
        if (properties is null || !properties.TryGetValue(Constants.QueueInfoProperties.DeadLetterRoutingKey, out var value))
            return null;

        return TypeConversion.TryConvertTo<string>(value, out var key) && !string.IsNullOrWhiteSpace(key) ? key : null;
    }

    /// <summary>Max priority from additional properties, or null when unset.</summary>
    public static int? MaxPriority(IReadOnlyDictionary<string, object>? properties)
    {
        if (properties is null || !properties.TryGetValue(Constants.QueueInfoProperties.MaxPriority, out var value))
            return null;

        return TypeConversion.TryConvertTo<int>(value, out var parsed) ? parsed : null;
    }

    /// <summary>True for the unnamed default exchange and broker-reserved <c>amq.*</c> names.</summary>
    public static bool IsDefaultExchange(string? name)
        => string.IsNullOrEmpty(name) || name.StartsWith("amq.", StringComparison.Ordinal);

    private static bool ReadBool(IReadOnlyDictionary<string, object>? properties, string key)
        => properties is not null && properties.TryGetValue(key, out var value) && value is true;
}
