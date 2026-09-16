using Lyo.Web.Primitives;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.Tests;

public sealed class FileStorageColorHelperTests
{
    public static TheoryData<string, LyoChipHue> TypeNames()
    {
        var data = new TheoryData<string, LyoChipHue>();
        data.Add("sheet.csv", LyoChipHue.Teal);
        data.Add("page.html", LyoChipHue.Purple);
        data.Add("photo.png", LyoChipHue.Cyan);
        data.Add("notes.pdf", LyoChipHue.Magenta);
        data.Add("rows.json", LyoChipHue.Magenta);
        data.Add("song.mp3", LyoChipHue.Violet);
        data.Add("clip.mp4", LyoChipHue.Amber);
        data.Add("bundle.zip", LyoChipHue.Green);
        data.Add("secret.gpg", LyoChipHue.Blue);
        data.Add("lib.nupkg", LyoChipHue.Rose);
        return data;
    }

    [Theory]
    [MemberData(nameof(TypeNames))]
    public void FileTypeChip_TypeName_UsesHue(string fileName, LyoChipHue hue)
    {
        var spec = FileStorageColorHelper.FileTypeChip(new Dictionary<string, object?> { ["OriginalFileName"] = fileName });
        Assert.Equal(hue.Style, spec.Style);
        Assert.Equal(Color.Default, spec.Color);
    }

    [Fact]
    public void FileTypeChip_CsvAndHtml_UseDifferentHues()
    {
        var csv = FileStorageColorHelper.FileTypeChip(new Dictionary<string, object?> { ["OriginalFileName"] = "sheet.csv" });
        var html = FileStorageColorHelper.FileTypeChip(new Dictionary<string, object?> { ["OriginalFileName"] = "page.html" });
        Assert.Equal("CSV", csv.Label);
        Assert.Equal("HTML", html.Label);
        Assert.NotEqual(csv.Style, html.Style);
    }

    [Fact]
    public void FileTypeChip_Unknown_HasNoHue()
    {
        var spec = FileStorageColorHelper.FileTypeChip(new Dictionary<string, object?> { ["OriginalFileName"] = "blob.nope" });
        Assert.Equal("File", spec.Label);
        Assert.Null(spec.Style);
        Assert.Equal(Color.Default, spec.Color);
    }

    [Fact]
    public void EncryptionChip_Aes_IsBlue_AndBlank_IsNone()
    {
        var aes = FileStorageColorHelper.EncryptionChip("AES-GCM");
        Assert.Equal(LyoChipHue.Blue.Style, aes.Style);
        Assert.Equal("AES-GCM", aes.Label);

        var none = FileStorageColorHelper.EncryptionChip(" ");
        Assert.Equal("None", none.Label);
        Assert.Null(none.Style);
    }

    [Fact]
    public void CompressionChip_Brotli_IsLime_AndBlank_IsNone()
    {
        var brotli = FileStorageColorHelper.CompressionChip("brotli");
        Assert.Equal(LyoChipHue.Lime.Style, brotli.Style);
        Assert.Equal("brotli", brotli.Label);

        var none = FileStorageColorHelper.CompressionChip(null);
        Assert.Equal("None", none.Label);
        Assert.Null(none.Style);
    }
}
