namespace Lyo.Desktop.Components.Tests;

public class FileKindTests
{
    [Fact]
    public void Glyph_UsesTheExtension()
    {
        Assert.Equal(FileGlyph.Folder, FileKind.Glyph("docs", true));
        Assert.Equal(FileGlyph.Image, FileKind.Glyph("shot.PNG", false));
        Assert.Equal(FileGlyph.Document, FileKind.Glyph("notes.txt", false));
        Assert.Equal(FileGlyph.File, FileKind.Glyph("blob", false));
    }

    [Fact]
    public void TypeChip_PrefersTheMediaTypeThenTheExtension()
    {
        Assert.Equal("Folder", FileKind.TypeChip(null, "docs", true).Label);
        Assert.Equal(ChipHues.Amber, FileKind.TypeChip(null, "docs", true).Hue);
        Assert.Equal("PDF", FileKind.TypeChip("application/pdf", "ignored.bin", false).Label);
        Assert.Equal("TXT", FileKind.TypeChip(null, "notes.txt", false).Label);
    }

    [Fact]
    public void CompressionChip_ColorsKnownAlgorithmsAndHidesDirectories()
    {
        Assert.Null(FileKind.CompressionChip(true, true, "GZip"));
        Assert.Equal("None", FileKind.CompressionChip(false, false, null)!.Value.Label);
        Assert.Equal(ChipHues.Lime, FileKind.CompressionChip(false, true, "Brotli")!.Value.Hue);
        Assert.Equal(ChipHues.Amber, FileKind.CompressionChip(false, true, "GZip")!.Value.Hue);
        Assert.Equal("GZip", FileKind.CompressionChip(false, true, "GZip")!.Value.Label);
    }

    [Fact]
    public void EncryptionChip_ColorsKnownAlgorithms()
    {
        Assert.Null(FileKind.EncryptionChip(true, true, "AesGcm"));
        Assert.Equal("None", FileKind.EncryptionChip(false, false, null)!.Value.Label);
        Assert.Equal(ChipHues.Blue, FileKind.EncryptionChip(false, true, "AesGcm")!.Value.Hue);
        Assert.Equal(ChipHues.Teal, FileKind.EncryptionChip(false, true, "ChaCha20-Poly1305")!.Value.Hue);
    }

    [Fact]
    public void Fill_LimeIsMostlyGreen()
    {
        var color = ChipHues.Fill(ChipHues.Lime);
        Assert.True(color.G > color.R);
        Assert.True(color.G > color.B);
    }
}