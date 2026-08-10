using System.Text;
using Lyo.Common.Enums;
using Lyo.Testing;

namespace Lyo.TextEncoding.Tests;

public sealed class BinaryEncodingTests
{
    [Theory]
    [InlineData(BinaryEncodingKind.Base64)]
    [InlineData(BinaryEncodingKind.Base64Url)]
    [InlineData(BinaryEncodingKind.Hex)]
    public void EncodeDecode_RoundTrip_ReturnsOriginal(BinaryEncodingKind kind)
    {
        var data = TestData.Create(64);
        var encoded = BinaryEncoding.Encode(kind, data, TextLetterCase.Lower);
        var decoded = BinaryEncoding.Decode(kind, encoded);
        Assert.Equal(data, decoded);
    }

    [Fact]
    public void Encode_Empty_ReturnsEmptyString()
        => Assert.Equal(string.Empty, BinaryEncoding.Encode(BinaryEncodingKind.Base64, ReadOnlySpan<byte>.Empty));

    [Fact]
    public void TryEncode_DestinationTooSmall_ReturnsFalse()
    {
        var data = TestData.Create(8);
        Span<char> dest = stackalloc char[2];
        Assert.False(BinaryEncoding.TryEncode(BinaryEncodingKind.Hex, data, dest, out _));
    }

    [Fact]
    public void TryDecode_InvalidHex_ReturnsFalse()
        => Assert.False(BinaryEncoding.TryDecode(BinaryEncodingKind.Hex, "zz", out byte[]? _));

    [Fact]
    public void Encode_Base64Url_OmitsPaddingAndUsesUrlAlphabet()
    {
        var data = TestData.Create(5);
        var url = BinaryEncoding.Encode(BinaryEncodingKind.Base64Url, data);
        Assert.DoesNotContain('=', url);
        Assert.DoesNotContain('+', url);
        Assert.DoesNotContain('/', url);
        Assert.Equal(data, BinaryEncoding.Decode(BinaryEncodingKind.Base64Url, url));
    }

    [Fact]
    public void Encode_LineLength76_InsertsCrlf()
    {
        var data = TestData.Create(80);
        var encoded = BinaryEncoding.Encode(BinaryEncodingKind.Base64, data, lineLength: 76);
        Assert.Contains("\r\n", encoded);
        Assert.Equal(data, BinaryEncoding.Decode(BinaryEncodingKind.Base64, encoded));
    }

    [Fact]
    public void EncodePem_DecodePem_RoundTrip()
    {
        var data = TestData.Create(48);
        var pem = BinaryEncoding.EncodePem("CERTIFICATE", data);
        Assert.Contains("-----BEGIN CERTIFICATE-----", pem);
        Assert.Contains("-----END CERTIFICATE-----", pem);
        var decoded = BinaryEncoding.DecodePem(pem, out var label);
        Assert.Equal("CERTIFICATE", label);
        Assert.Equal(data, decoded);
    }

    [Fact]
    public async Task DecodeAsync_Stream_MatchesBufferDecode()
    {
        var data = TestData.Create(200);
        foreach (var kind in new[] { BinaryEncodingKind.Base64, BinaryEncodingKind.Base64Url, BinaryEncodingKind.Hex }) {
            var encoded = BinaryEncoding.Encode(kind, data, lineLength: kind == BinaryEncodingKind.Base64 ? 76 : 0);
            using var reader = new StringReader(encoded);
            await using var ms = new MemoryStream();
            await BinaryEncoding.DecodeAsync(kind, reader, ms, TestContext.Current.CancellationToken);
            Assert.Equal(data, ms.ToArray());
        }
    }

    [Fact]
    public async Task EncodeDecode_File_RoundTrip()
    {
        var data = TestData.Create(128);
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try {
            var bin = Path.Combine(dir, "in.bin");
            var enc = Path.Combine(dir, "out.txt");
            var outBin = Path.Combine(dir, "out.bin");
            await File.WriteAllBytesAsync(bin, data, TestContext.Current.CancellationToken);
            await BinaryEncoding.EncodeFileAsync(BinaryEncodingKind.Base64, bin, enc, ct: TestContext.Current.CancellationToken);
            await BinaryEncoding.DecodeFileAsync(BinaryEncodingKind.Base64, enc, outBin, TestContext.Current.CancellationToken);
            Assert.Equal(data, await File.ReadAllBytesAsync(outBin, TestContext.Current.CancellationToken));
        }
        finally {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Service_Shared_HonorsDefaultHexCase()
    {
        var data = new byte[] { 0xAB };
        Assert.Equal("AB", BinaryEncodingService.Shared.Encode(BinaryEncodingKind.Hex, data));
    }

    [Fact]
    public void Namespace_AllowsSystemTextEncodingWithoutAlias()
    {
        // Compiles in Lyo.TextEncoding.Tests without TextEncoding alias — Encoding means BCL.
        Encoding utf8 = Encoding.UTF8;
        Assert.Equal(65001, utf8.CodePage);
    }
}
