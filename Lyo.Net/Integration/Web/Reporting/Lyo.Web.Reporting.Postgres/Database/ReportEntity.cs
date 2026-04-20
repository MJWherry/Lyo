using System.ComponentModel.DataAnnotations;

namespace Lyo.Web.Reporting.Postgres.Database;

/// <summary>Entity representing a saved report in the database.</summary>
public sealed class ReportEntity
{
    /// <summary>Gets or sets the unique identifier.</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the report name/title.</summary>
    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = null!;

    /// <summary>Gets or sets the report description.</summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Gets or sets the serialized report data as JSON.</summary>
    [Required]
    public string ReportDataJson { get; set; } = null!;

    /// <summary>Gets or sets the type name of the report parameters (for deserialization).</summary>
    [MaxLength(500)]
    public string? ParameterTypeName { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the last update timestamp.</summary>
    [Required]
    public DateTime UpdatedTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets custom metadata tags (comma-separated or JSON array).</summary>
    [MaxLength(1000)]
    public string? Tags { get; set; }

    /// <summary>Gets or sets whether the report is active (not deleted).</summary>
    public bool IsActive { get; set; } = true;
}