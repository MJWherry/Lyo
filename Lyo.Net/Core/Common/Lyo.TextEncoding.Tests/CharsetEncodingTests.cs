using System.Text;

namespace Lyo.TextEncoding.Tests;

public sealed class CharsetEncodingTests
{
    public CharsetEncodingTests() => CharsetEncoding.EnsureCodePagesRegistered();

    [Fact]
    public void GetBytesGetString_Utf8_RoundTrip()
    {
        const string text = "hello café";
        var bytes = CharsetEncoding.GetBytes(text, Encoding.UTF8);
        Assert.Equal(text, CharsetEncoding.GetString(bytes, Encoding.UTF8));
    }

    [Fact]
    public void GetBytesGetString_Windows1252_RoundTrip()
    {
        const string text = "naïve — résumé";
        var enc = CharsetEncoding.GetEncoding(CharsetInfo.Windows1252);
        var bytes = CharsetEncoding.GetBytes(text, enc);
        Assert.Equal(text, CharsetEncoding.GetString(bytes, enc));
    }

    [Fact]
    public void Convert_Windows1252ToUtf8_PreservesText()
    {
        const string text = "café";
        var win = CharsetEncoding.GetEncoding(CharsetInfo.Windows1252);
        var bytes1252 = win.GetBytes(text);
        var utf8Bytes = CharsetEncoding.Convert(bytes1252, CharsetInfo.Windows1252, CharsetInfo.Utf8);
        Assert.Equal(text, Encoding.UTF8.GetString(utf8Bytes));
    }

    [Fact]
    public async Task Convert_StreamSync_MatchesAsync()
    {
        const string text = "stream sync";
        var utf16 = Encoding.Unicode.GetBytes(text);
        using var input1 = new MemoryStream(utf16);
        using var out1 = new MemoryStream();
        CharsetEncoding.Convert(input1, out1, CharsetInfo.Utf16Le, CharsetInfo.Utf8);
        using var input2 = new MemoryStream(utf16);
        using var out2 = new MemoryStream();
        await CharsetEncoding.ConvertAsync(input2, out2, CharsetInfo.Utf16Le, CharsetInfo.Utf8, ct: TestContext.Current.CancellationToken);
        Assert.Equal(out1.ToArray(), out2.ToArray());
        Assert.Equal(text, Encoding.UTF8.GetString(out1.ToArray()));
    }

    [Fact]
    public void DetectEncoding_Utf8Bom_ReturnsBom()
    {
        byte[] data = [0xEF, 0xBB, 0xBF, (byte)'a'];
        var result = CharsetEncoding.DetectEncoding(data);
        Assert.Equal(CharsetDetectionKind.Bom, result.Kind);
        Assert.Equal(CharsetInfo.Utf8, result.Charset);
        Assert.Empty(result.ConsumedPrefix);
    }

    [Fact]
    public void DetectEncoding_ValidUtf8NoBom_ReturnsUtf8Heuristic()
    {
        var data = Encoding.UTF8.GetBytes("hello world");
        var result = CharsetEncoding.DetectEncoding(data);
        Assert.Equal(CharsetDetectionKind.Utf8Heuristic, result.Kind);
    }

    [Fact]
    public void DetectEncoding_Empty_ReturnsDefault()
    {
        var result = CharsetEncoding.DetectEncoding([]);
        Assert.Equal(CharsetDetectionKind.Default, result.Kind);
        Assert.Equal(CharsetInfo.Utf8, result.Charset);
    }

    [Fact]
    public void DetectEncoding_NonSeekable_SetsConsumedPrefix_AndReplayPreservesPayload()
    {
        var payload = Encoding.UTF8.GetBytes("hello non-seekable stream payload");
        using var inner = new NonSeekableMemoryStream(payload);
        var detection = CharsetEncoding.DetectEncoding(inner);
        Assert.NotEmpty(detection.ConsumedPrefix);
        using var replay = CharsetEncoding.CreateReplayStream(inner, detection);
        using var ms = new MemoryStream();
        replay.CopyTo(ms);
        Assert.Equal(payload, ms.ToArray());
    }

    [Fact]
    public void DetectEncodingFromText_XmlDeclaration_ReturnsTextDeclaration()
    {
        const string xml = """<?xml version="1.0" encoding="windows-1252"?><root/>""";
        var result = CharsetEncoding.DetectEncodingFromText(xml);
        Assert.Equal(CharsetDetectionKind.TextDeclaration, result.Kind);
        Assert.Equal("windows-1252", result.DeclaredName);
        Assert.Equal(1252, result.Encoding.CodePage);
    }

    [Fact]
    public void CharsetInfo_FromWebName_Alias_ReturnsWellKnown() => Assert.Same(CharsetInfo.Windows1252, CharsetInfo.FromWebName("cp1252"));

    [Fact]
    public void CharsetInfo_Custom_NotInWellKnown_ButResolves()
    {
        var custom = CharsetInfo.Custom("MyUtf8", "utf-8", 65001);
        Assert.DoesNotContain(custom, CharsetInfo.WellKnown);
        Assert.Equal(65001, custom.ToEncoding().CodePage);
    }

    [Fact]
    public void GetEncoding_UnknownName_ThrowsEncodingException() => Assert.Throws<EncodingException>(() => CharsetEncoding.GetEncoding("not-a-real-charset-xyz"));

    [Fact]
    public void DecoderFallback_Replacement_DoesNotThrow()
    {
        var options = new CharsetEncodingOptions { DecoderFallback = DecoderFallback.ReplacementFallback };
        var encoding = CharsetEncoding.GetEncoding(CharsetInfo.Utf8, options);
        byte[] bad = [0x80];
        var s = encoding.GetString(bad);
        Assert.NotEmpty(s);
        Assert.Same(Encoding.UTF8.DecoderFallback, Encoding.UTF8.DecoderFallback);
    }

    [Fact]
    public void ExceptionFallback_InvalidUtf8_Throws()
    {
        var options = new CharsetEncodingOptions { DecoderFallback = DecoderFallback.ExceptionFallback };
        var encoding = CharsetEncoding.GetEncoding(CharsetInfo.Utf8, options);
        byte[] bad = [0x80];
        Assert.ThrowsAny<ArgumentException>(() => encoding.GetString(bad));
        Assert.NotSame(DecoderFallback.ExceptionFallback, Encoding.UTF8.DecoderFallback);
    }

    [Fact]
    public async Task WriteAllTextAsync_EmitBom_WritesUtf8Preamble()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
        try {
            await CharsetEncoding.WriteAllTextAsync(path, "hi", Encoding.UTF8, true, ct: TestContext.Current.CancellationToken);
            var bytes = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
            Assert.Equal(0xEF, bytes[0]);
            Assert.Equal(0xBB, bytes[1]);
            Assert.Equal(0xBF, bytes[2]);
        }
        finally {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WriteAllTextAsync_Default_OmitsBom()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
        try {
            await CharsetEncoding.WriteAllTextAsync(path, "hi", Encoding.UTF8, false, ct: TestContext.Current.CancellationToken);
            var bytes = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
            Assert.Equal((byte)'h', bytes[0]);
        }
        finally {
            File.Delete(path);
        }
    }

    [Fact]
    public void CharsetConvertingStream_Write_Converts1252ToUtf8()
    {
        const string text = "café";
        var win = CharsetEncoding.GetEncoding(CharsetInfo.Windows1252);
        var src = win.GetBytes(text);
        using var sink = new MemoryStream();
        using (var converting = CharsetEncoding.CreateConvertingStream(sink, CharsetInfo.Windows1252, CharsetInfo.Utf8))
            converting.Write(src, 0, src.Length);

        Assert.Equal(text, Encoding.UTF8.GetString(sink.ToArray()));
    }

    [Fact]
    public async Task ConvertFileAsync_RoundTrip()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try {
            var input = Path.Combine(dir, "in.txt");
            var output = Path.Combine(dir, "out.txt");
            const string text = "hello";
            await File.WriteAllBytesAsync(input, Encoding.Unicode.GetBytes(text), TestContext.Current.CancellationToken);
            await CharsetEncoding.ConvertFileAsync(input, output, CharsetInfo.Utf16Le, CharsetInfo.Utf8, ct: TestContext.Current.CancellationToken);
            Assert.Equal(text, Encoding.UTF8.GetString(await File.ReadAllBytesAsync(output, TestContext.Current.CancellationToken)));
        }
        finally {
            Directory.Delete(dir, true);
        }
    }

    private sealed class NonSeekableMemoryStream(byte[] data)
        : MemoryStream(data)
    {
        public override bool CanSeek => false;

        public override long Position {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();
    }
}