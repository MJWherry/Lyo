using Lyo.IO.Temp.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace Lyo.FileTags.Images.Tests;

public class ImageFileTagProviderTests
{
    [Fact]
    public async Task ImageRead_MapsExifArtist()
    {
        using var session = new IOTempSession();
        var path = session.GetFilePath("photo.jpg");
        using (var image = new Image<Rgba32>(8, 8)) {
            image.Metadata.ExifProfile = new();
            image.Metadata.ExifProfile.SetValue(ExifTag.Artist, "Ada");
            await image.SaveAsJpegAsync(path, TestCt);
        }

        var read = await new ImageFileTagProvider().ReadAsync(path, TestCt);
        Assert.True(read.IsSuccess, read.Errors?.FirstOrDefault()?.Message);
        Assert.Equal("Ada", read.Data!.Tags[FileTagKey.Artist]);
    }
}