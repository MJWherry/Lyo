using Lyo.Recipe.Models.Enums;

namespace Lyo.Recipe.Models.Response;

/// <summary>FileStorage attachment as returned by the API.</summary>
public sealed class RecipeAttachmentRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Subject type from <see cref="RecipeEntityTypes" />.</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Subject id.</summary>
    public Guid EntityId { get; set; }

    /// <summary>FileStorage file id.</summary>
    public Guid FileId { get; set; }

    /// <summary>How the file relates to the subject.</summary>
    public RecipeAttachmentRole Role { get; set; }

    /// <summary>Optional caption.</summary>
    public string? Caption { get; set; }

    /// <summary>When true this is the primary file for the subject.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Display order among attachments.</summary>
    public int SortOrder { get; set; }

    /// <summary>Created (UTC).</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Last write (UTC).</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}