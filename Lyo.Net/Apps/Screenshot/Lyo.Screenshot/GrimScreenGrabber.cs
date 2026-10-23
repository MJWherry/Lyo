using System.Diagnostics;
using System.Text;
using Lyo.Exceptions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Lyo.Screenshot;

/// <summary>Grabs a rectangle by running <c>grim</c>. A zero-size region captures every output. The compositor decides whether the pointer is in the image.</summary>
internal sealed class GrimScreenGrabber : IScreenGrabber
{
    public static bool IsAvailable => CommandExists("grim");

    public CaptureRegion VirtualScreen => new(0, 0, 0, 0);

    public Image<Rgba32> Grab(CaptureRegion region, bool includeCursor)
    {
        var start = new ProcessStartInfo {
            FileName = "grim",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (region.Width > 0 && region.Height > 0) {
            start.ArgumentList.Add("-g");
            start.ArgumentList.Add($"{region.X},{region.Y} {region.Width}x{region.Height}");
        }

        start.ArgumentList.Add("-t");
        start.ArgumentList.Add("ppm");
        start.ArgumentList.Add("-");
        using var process = new Process { StartInfo = start };
        try {
            if (!process.Start())
                throw new InvalidOperationException("Could not start grim.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException) {
            throw new InvalidOperationException($"Could not start grim: {ex.Message}", ex);
        }

        using var copy = new MemoryStream();
        process.StandardOutput.BaseStream.CopyTo(copy);
        process.WaitForExit();
        if (process.ExitCode != 0) {
            var err = process.StandardError.ReadToEnd().Trim();
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(err) ? "grim failed." : err);
        }

        return ReadPpm(copy.ToArray());
    }

    public void Dispose() { }

    private static Image<Rgba32> ReadPpm(byte[] data)
    {
        var index = 0;
        var magic = NextToken(data, ref index);
        OperationHelpers.ThrowIf(magic != "P6", "grim did not return a P6 image.");
        var width = int.Parse(NextToken(data, ref index));
        var height = int.Parse(NextToken(data, ref index));
        var max = int.Parse(NextToken(data, ref index));
        OperationHelpers.ThrowIf(max != 255, "grim PPM max value must be 255.");
        if (index < data.Length && data[index] is (byte)' ' or (byte)'\n' or (byte)'\r' or (byte)'\t')
            index++;

        var needed = width * height * 3;
        OperationHelpers.ThrowIf(index + needed > data.Length, "grim returned a short PPM.");
        var pixels = new Rgba32[width * height];
        for (var i = 0; i < pixels.Length; i++) {
            var offset = index + i * 3;
            pixels[i] = new(data[offset], data[offset + 1], data[offset + 2], 255);
        }

        return Image.LoadPixelData(pixels, width, height);
    }

    private static string NextToken(byte[] data, ref int index)
    {
        while (index < data.Length) {
            if (data[index] == (byte)'#') {
                while (index < data.Length && data[index] != (byte)'\n')
                    index++;

                continue;
            }

            if (data[index] is (byte)' ' or (byte)'\n' or (byte)'\r' or (byte)'\t') {
                index++;
                continue;
            }

            var start = index;
            while (index < data.Length && data[index] is not ((byte)' ' or (byte)'\n' or (byte)'\r' or (byte)'\t'))
                index++;

            return Encoding.ASCII.GetString(data, start, index - start);
        }

        throw new InvalidOperationException("grim returned a short PPM.");
    }

    private static bool CommandExists(string name)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
            return false;

        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)) {
            if (File.Exists(Path.Combine(dir, name)))
                return true;
        }

        return false;
    }
}