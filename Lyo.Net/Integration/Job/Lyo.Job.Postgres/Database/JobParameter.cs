namespace Lyo.Job.Postgres.Database;

public class JobParameter
{
    public Guid Id { get; set; }

    public Guid JobDefinitionId { get; set; }

    public string? Description { get; set; }

    public string Key { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? Value { get; set; }

    public byte[]? EncryptedValue { get; set; }

    public bool AllowMultiple { get; set; }

    public bool Required { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual JobDefinition JobDefinition { get; set; } = null!;
}