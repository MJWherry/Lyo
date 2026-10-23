using Lyo.Book.Postgres.Database;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Book.Postgres;

/// <summary>Idempotent lookup seed for book types, formats, and a hierarchical genre tree.</summary>
public static class BookDbSeeder
{
    public static readonly Guid FictionTypeId = Guid.Parse("6a1c0b20-1c3a-4d7e-9f11-0b8c2d4e5f60");
    public static readonly Guid HardcoverFormatId = Guid.Parse("7b2d1c31-2d4b-4e8f-a022-1c9d3e5f6071");
    public static readonly Guid FantasyGenreId = Guid.Parse("8c3e2d42-3e5c-4f90-b133-2d0e4f607182");

    public static async Task SeedAsync(BookDbContext db, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        await SeedTypesAsync(db, ct).ConfigureAwait(false);
        await SeedFormatsAsync(db, ct).ConfigureAwait(false);
        await SeedGenresAsync(db, ct).ConfigureAwait(false);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static async Task SeedTypesAsync(BookDbContext db, CancellationToken ct)
    {
        (Guid Id, string Slug, string Name, int Sort)[] types = [
            (FictionTypeId, "fiction", "Fiction", 1), (Guid.Parse("6a1c0b20-1c3a-4d7e-9f11-0b8c2d4e5f61"), "non-fiction", "Non-fiction", 2),
            (Guid.Parse("6a1c0b20-1c3a-4d7e-9f11-0b8c2d4e5f62"), "textbook", "Textbook", 3), (Guid.Parse("6a1c0b20-1c3a-4d7e-9f11-0b8c2d4e5f63"), "reference", "Reference", 4),
            (Guid.Parse("6a1c0b20-1c3a-4d7e-9f11-0b8c2d4e5f64"), "poetry", "Poetry", 5), (Guid.Parse("6a1c0b20-1c3a-4d7e-9f11-0b8c2d4e5f65"), "anthology", "Anthology", 6),
            (Guid.Parse("6a1c0b20-1c3a-4d7e-9f11-0b8c2d4e5f66"), "childrens", "Children's", 7), (Guid.Parse("6a1c0b20-1c3a-4d7e-9f11-0b8c2d4e5f67"), "academic", "Academic", 8),
            (Guid.Parse("6a1c0b20-1c3a-4d7e-9f11-0b8c2d4e5f68"), "graphic-narrative", "Graphic narrative", 9)
        ];

        var existing = await db.Types.Select(t => t.Slug).ToHashSetAsync(ct).ConfigureAwait(false);
        foreach (var (id, slug, name, sort) in types) {
            if (existing.Contains(slug))
                continue;

            db.Types.Add(
                new() {
                    Id = id,
                    Slug = slug,
                    Name = name,
                    SortOrder = sort
                });
        }
    }

    private static async Task SeedFormatsAsync(BookDbContext db, CancellationToken ct)
    {
        (Guid Id, string Slug, string Name, int Sort)[] formats = [
            (HardcoverFormatId, "hardcover", "Hardcover", 1), (Guid.Parse("7b2d1c31-2d4b-4e8f-a022-1c9d3e5f6072"), "paperback", "Paperback", 2),
            (Guid.Parse("7b2d1c31-2d4b-4e8f-a022-1c9d3e5f6073"), "trade-paperback", "Trade paperback", 3),
            (Guid.Parse("7b2d1c31-2d4b-4e8f-a022-1c9d3e5f6074"), "ebook", "Ebook", 4), (Guid.Parse("7b2d1c31-2d4b-4e8f-a022-1c9d3e5f6075"), "audiobook", "Audiobook", 5),
            (Guid.Parse("7b2d1c31-2d4b-4e8f-a022-1c9d3e5f6076"), "large-print", "Large print", 6),
            (Guid.Parse("7b2d1c31-2d4b-4e8f-a022-1c9d3e5f6077"), "board-book", "Board book", 7), (Guid.Parse("7b2d1c31-2d4b-4e8f-a022-1c9d3e5f6078"), "manuscript", "Manuscript", 8)
        ];

        var existing = await db.Formats.Select(t => t.Slug).ToHashSetAsync(ct).ConfigureAwait(false);
        foreach (var (id, slug, name, sort) in formats) {
            if (existing.Contains(slug))
                continue;

            db.Formats.Add(
                new() {
                    Id = id,
                    Slug = slug,
                    Name = name,
                    SortOrder = sort
                });
        }
    }

    private static async Task SeedGenresAsync(BookDbContext db, CancellationToken ct)
    {
        var fiction = Guid.Parse("8c3e2d42-3e5c-4f90-b133-2d0e4f607180");
        var nonFiction = Guid.Parse("8c3e2d42-3e5c-4f90-b133-2d0e4f607181");
        (Guid Id, Guid? ParentId, string Slug, string Name, int Sort)[] genres = [
            (fiction, null, "fiction", "Fiction", 1), (nonFiction, null, "non-fiction", "Non-fiction", 2), (FantasyGenreId, fiction, "fantasy", "Fantasy", 10),
            (Guid.Parse("8c3e2d42-3e5c-4f90-b133-2d0e4f607183"), fiction, "mystery", "Mystery", 11),
            (Guid.Parse("8c3e2d42-3e5c-4f90-b133-2d0e4f607184"), fiction, "science-fiction", "Science fiction", 12),
            (Guid.Parse("8c3e2d42-3e5c-4f90-b133-2d0e4f607185"), nonFiction, "biography", "Biography", 20),
            (Guid.Parse("8c3e2d42-3e5c-4f90-b133-2d0e4f607186"), nonFiction, "history", "History", 21),
            (Guid.Parse("8c3e2d42-3e5c-4f90-b133-2d0e4f607187"), nonFiction, "science", "Science", 22)
        ];

        var existing = await db.Genres.Select(t => t.Slug).ToHashSetAsync(ct).ConfigureAwait(false);
        foreach (var (id, parentId, slug, name, sort) in genres) {
            if (existing.Contains(slug))
                continue;

            db.Genres.Add(
                new() {
                    Id = id,
                    ParentGenreId = parentId,
                    Slug = slug,
                    Name = name,
                    SortOrder = sort
                });
        }
    }
}