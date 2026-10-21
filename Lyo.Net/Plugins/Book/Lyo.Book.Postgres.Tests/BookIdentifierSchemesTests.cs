namespace Lyo.Book.Postgres.Tests;

[Trait("Category", "Fast")]
public sealed class BookIdentifierSchemesTests
{
    [Fact]
    public void Normalize_Isbn13WithHyphens_StripsNonDigits() => Assert.Equal("9780143127550", BookIdentifierSchemes.Normalize(BookIdentifierSchemes.Isbn13, "978-0-14-312755-0"));

    [Fact]
    public void Normalize_Isbn10WithX_KeepsCheckDigit() => Assert.Equal("080442957X", BookIdentifierSchemes.Normalize(BookIdentifierSchemes.Isbn10, "0-8044-2957-X"));

    [Fact]
    public void Normalize_NonIsbn_TrimsOnly() => Assert.Equal("OL123M", BookIdentifierSchemes.Normalize(BookIdentifierSchemes.OpenLibrary, "  OL123M  "));
}