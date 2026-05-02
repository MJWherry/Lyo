namespace Lyo.Comic.Api.Models.Request;

/// <summary>Request model for creating or updating a comic page.</summary>
public sealed class ComicPageReq
{
    /// <summary>Gets or sets the chapter this page belongs to.</summary>
    public Guid ChapterId { get; set; }

    /// <summary>Gets or sets the 1-based page number within the chapter.</summary>
    public int PageNumber { get; set; }

    /// <summary>Gets or sets a reference to the page image in file storage.</summary>
    public string? ImageRef { get; set; }

    /// <summary>Gets or sets the pixel width of the page image, if known.</summary>
    public int? Width { get; set; }

    /// <summary>Gets or sets the pixel height of the page image, if known.</summary>
    public int? Height { get; set; }
}
