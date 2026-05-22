using Lyo.Images.OpenCv;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Lyo.Images.OpenCv.Tests;

public sealed class OpenCvRoiInpaintTests
{
    [Fact]
    public void InpaintColorRoiPng_invalid_png_returns_decode_failed()
    {
        var r = OpenCvRoiInpaint.InpaintTelea(new byte[] { 0x00, 0x01, 0x02 }, 0, 0, 4, 4, 3);
        Assert.False(r.IsSuccess);
        Assert.NotNull(r.Errors);
        Assert.Equal("OpenCvInpaint.DecodeFailed", r.Errors[0].Code);
    }

    [Fact]
    public async Task InpaintTelea_valid_png_returns_same_dimensions_and_changes_inpainted_region()
    {
        using var img = new Image<Rgba32>(48, 48, Color.White);
        for (var y = 16; y < 32; y++) {
            for (var x = 16; x < 32; x++)
                img[x, y] = Color.Red;
        }

        await using var ms = new MemoryStream();
        await img.SaveAsPngAsync(ms, TestContext.Current.CancellationToken);
        var png = ms.ToArray();

        var r = OpenCvRoiInpaint.InpaintTelea(png, 16, 16, 16, 16, 5);
        Assert.True(r.IsSuccess, r.Errors is { Count: > 0 } ? r.Errors[0].Message : "expected success");
        Assert.NotNull(r.Data);

        using var outImg = await Image.LoadAsync<Rgba32>(new MemoryStream(r.Data), TestContext.Current.CancellationToken);
        Assert.Equal(48, outImg.Width);
        Assert.Equal(48, outImg.Height);

        var before = img[24, 24];
        var after = outImg[24, 24];
        Assert.True(before.R > 240 && before.G < 20);
        var sumBefore = (int)before.R + before.G + before.B;
        var sumAfter = (int)after.R + after.G + after.B;
        Assert.True(sumAfter > sumBefore + 80, $"expected inpaint to pull toward surrounding white, before={sumBefore} after={sumAfter}");
    }

    [Fact]
    public async Task InpaintColorRoiPng_navier_stokes_matches_dimensions_and_changes_region()
    {
        using var img = new Image<Rgba32>(48, 48, Color.White);
        for (var y = 16; y < 32; y++) {
            for (var x = 16; x < 32; x++)
                img[x, y] = Color.Red;
        }

        await using var ms = new MemoryStream();
        await img.SaveAsPngAsync(ms, TestContext.Current.CancellationToken);
        var png = ms.ToArray();

        var r = OpenCvRoiInpaint.InpaintColorRoiPng(png, 16, 16, 16, 16, 5, OpenCvInpaintAlgorithm.NavierStokes);
        Assert.True(r.IsSuccess, r.Errors is { Count: > 0 } ? r.Errors[0].Message : "expected success");
        Assert.NotNull(r.Data);

        using var outImg = await Image.LoadAsync<Rgba32>(new MemoryStream(r.Data), TestContext.Current.CancellationToken);
        Assert.Equal(48, outImg.Width);
        Assert.Equal(48, outImg.Height);

        var before = img[24, 24];
        var after = outImg[24, 24];
        var sumBefore = (int)before.R + before.G + before.B;
        var sumAfter = (int)after.R + after.G + after.B;
        Assert.True(sumAfter > sumBefore + 80);
    }

    [Fact]
    public void InpaintTelea_clamps_mask_to_image_bounds()
    {
        using var img = new Image<Rgba32>(10, 10, Color.Blue);
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);
        var png = ms.ToArray();

        var r = OpenCvRoiInpaint.InpaintTelea(png, -5, -5, 100, 100, 2);
        Assert.True(r.IsSuccess, r.Errors is { Count: > 0 } ? r.Errors[0].Message : "");
        Assert.NotNull(r.Data);
    }

    [Fact]
    public void AddOpenCvRoiInpaint_resolves_IOpenCvRoiInpaint()
    {
        var services = new ServiceCollection();
        services.AddOpenCvRoiInpaint();
        using var sp = services.BuildServiceProvider();
        var impl = sp.GetRequiredService<IOpenCvRoiInpaint>();
        Assert.IsType<OpenCvRoiInpaintService>(impl);
    }

    [Fact]
    public void AddOpenCvRoiInpaint_does_not_duplicate_registration()
    {
        var services = new ServiceCollection();
        services.AddOpenCvRoiInpaint();
        services.AddOpenCvRoiInpaint();
        using var sp = services.BuildServiceProvider();
        var a = sp.GetRequiredService<IOpenCvRoiInpaint>();
        var b = sp.GetRequiredService<IOpenCvRoiInpaint>();
        Assert.Same(a, b);
    }
}
