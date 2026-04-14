namespace Lyo.Config;

/// <summary>Immutable snapshot of a <see cref="ConfigBindingRecord" /> value at a point in time (monotonic <see cref="Revision" /> per binding).</summary>
public sealed class ConfigBindingRevisionRecord
{
    /// <summary>Parent binding id (with <see cref="Revision" />, forms the store key).</summary>
    public Guid BindingId { get; set; }

    /// <summary>1-based revision number for this binding.</summary>
    public int Revision { get; set; }

    /// <summary>Typed value at this revision.</summary>
    public ConfigValue Value { get; set; } = new();

    /// <summary>When this revision was recorded.</summary>
    public DateTime CreatedTimestamp { get; set; }
}