using Lyo.FileStorage;

namespace Lyo.FileStorage.Tests;

public sealed class FileStorageArchivePathTests
{
    [Fact]
    public void SanitizeZipFileName_AddsZipExtension()
        => Assert.Equal("chapter.zip", FileStorageArchivePath.SanitizeZipFileName("chapter"));

    [Fact]
    public void SanitizeZipFileName_StripsPathSegments()
        => Assert.Equal("Q1-reports.zip", FileStorageArchivePath.SanitizeZipFileName("exports/Q1-reports.zip"));

    [Fact]
    public void NormalizeZipPath_RejectsParentSegments()
        => Assert.Throws<ArgumentException>(() => FileStorageArchivePath.NormalizeZipPath("../x", Guid.NewGuid()));

    [Fact]
    public void NormalizeZipPath_UsesForwardSlashes()
        => Assert.Equal("Vol. 01/Ch. 001/001", FileStorageArchivePath.NormalizeZipPath(@"Vol. 01\Ch. 001\001", Guid.NewGuid()));

    [Fact]
    public void SanitizePathSegment_ReplacesInvalidChars()
        => Assert.Equal("a_b", FileStorageArchivePath.SanitizePathSegment("a/b"));
}
