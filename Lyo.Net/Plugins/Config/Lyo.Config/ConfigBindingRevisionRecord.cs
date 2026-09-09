namespace Lyo.Config;

/// <summary>Read-only snapshot of a <see cref="ConfigBindingRecord" /> value at a point in time (monotonic <see cref="Revision" /> per binding).</summary>
public sealed class ConfigBindingRevisionRecord
{
    /// <summary>Owning binding id (with <see cref="Revision" />, forms the store key).</summary>
    public Guid BindingId { get; set; }

    /// <summary>This binding 1-based revision number.</summary>
    public int Revision { get; set; }

    /// <summary>Strongly typed value at this revision.</summary>
    public ConfigValue Value { get; set; } = new();

    /// <summary>Timestamp of when this revision was written.</summary>
    public DateTime CreatedTimestamp { get; set; }
}