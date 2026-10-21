# Lyo.Book

Domain contracts for a work-first library catalog. A `Book` row is one edition (ISBN lives on the book). Optional `BookSeries`, lookup `BookType`/`BookFormat`/`BookGenre`
(hierarchical), `BookIdentifier` (catalog-wide unique scheme+normalized value), `BookContributor`, chapters, and page text. Persistence facade is `IBookStore`. Cross-feature
Favorite/Tag use `BookEntityTypes` (`Book`, `BookSeries`, `BookGenre`, `BookChapter`). Does not reference Favorite or Tag.

## Features

- **Catalog work.** `Book` with nullable series, type, and format; denormalized `PrimaryAuthor`; hydrated titles, identifiers, contributors, and genres.
- **Lookups.** First-class `BookType`, `BookFormat`, and hierarchical `BookGenre` records — not enums or free-form tags.
- **Identifiers.** `BookIdentifierSchemes` plus `Normalize` for ISBN digits. Unique `(scheme, normalized_value)` is enforced by the store.
- **Pages.** Raw searchable text (not Brotli). Domain `BookPage` has no tsvector. List APIs omit `Text` unless `includeText` is set.

## `IBookStore` responsibilities

- **Book.** `SaveBookAsync` upserts the work and replaces titles, identifiers, contributors, and the genre set; recomputes `PrimaryAuthor`. Get by id, slug, or identifier.
  `SearchBooksAsync` takes `BookQuery` (`FilterBookIds`: null = no filter, empty = no rows; `Take`/`Skip`; optional `FullText`). `DeleteBookAsync` cascades dependents.
- **Series.** Save (including series titles). Delete nulls `Book.SeriesId` and `SeriesIndex` and does not delete books.
- **Lookups.** Type, format, and genre CRUD. Delete is blocked while referenced. Genre parent cycles are rejected.
- **Chapters / pages.** Metadata CRUD. `GetPageTextAsync` / `SavePageTextAsync` own body text and roll up chapter and book word counts. `SearchPagesAsync` returns page hits with
  snippets.

## What is deliberately not here

- Authorization / tenancy.
- File/blob storage for covers and scans. Compose with FileStorage via `CoverImageRef` / `ScanFileRef`.
- Favorites, tags, ratings. Attach via `EntityRef` and `BookEntityTypes` at the host.
- Physical holdings, checkout, reading progress, Blazor UI.

## Layering map

| Assembly            | Responsibility                                                          |
|---------------------|-------------------------------------------------------------------------|
| `Lyo.Book` (this)   | POCOs + `IBookStore` (`netstandard2.0;net10.0`).                        |
| `Lyo.Book.Postgres` | EF `BookDbContext`, generated page `tsvector`, `PostgresBookStore`, DI. |