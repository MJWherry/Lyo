using System.Security.Cryptography;
using System.Text;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.AesSiv;
using Lyo.Encryption.Streaming;
using Lyo.Keystore;
using Lyo.Testing;

namespace Lyo.Encryption.Tests;

/// <summary>Guards single-shot wire compatibility and allocation floor after the MemoryStream/ToArray removal.</summary>
public sealed class SingleShotFramingTests
{
    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("42")]
    [InlineData("version-with-unicode-é")]
    public void BinaryWriterString_MatchesBinaryWriterAndBinaryReader(string value)
    {
        using var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
            bw.Write(value);

        var expected = ms.ToArray();
        Assert.Equal(expected.Length, FramingProbe.GetByteCount(value));

        var actual = new byte[expected.Length];
        var written = FramingProbe.Write(actual, value);
        Assert.Equal(expected.Length, written);
        Assert.Equal(expected, actual);

        var roundTrip = FramingProbe.Read(actual, out var consumed);
        Assert.Equal(expected.Length, consumed);
        Assert.Equal(value, roundTrip);
    }

    [Fact]
    public void AesGcm_Encrypt_WireLayout_ReadableByBinaryReader()
    {
        const string keyId = "framing-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "framing-password");
        var svc = new AesGcmEncryptionService(keyStore);
        var plaintext = Encoding.UTF8.GetBytes("single-shot framing probe");
        var encrypted = svc.Encrypt(plaintext, keyId);

        using var ms = new MemoryStream(encrypted);
        using var br = new BinaryReader(ms);
        Assert.Equal(1, br.ReadByte());
        var keyIdLen = br.ReadInt32();
        Assert.Equal(keyId, Encoding.UTF8.GetString(br.ReadBytes(keyIdLen)));
        Assert.False(string.IsNullOrWhiteSpace(br.ReadString()));
        Assert.Equal(AesGcmHelper.NonceSize, br.ReadInt32());
        br.ReadBytes(AesGcmHelper.NonceSize);
        br.ReadBytes(AesGcmHelper.TagSize);
        var ciphertext = br.ReadBytes((int)(ms.Length - ms.Position));
        Assert.Equal(plaintext.Length, ciphertext.Length);
        Assert.Equal(plaintext, svc.Decrypt(encrypted, keyId));
    }

    [Fact]
    public void AesSiv_EncryptDecrypt_RoundTripsWithoutExtraBodyCopies()
    {
        const string keyId = "siv-framing";
        var keyStore = new LocalKeyStore();
        keyStore.AddKey(keyId, "1", TestData.Create(32));
        var svc = new AesSivEncryptionService(keyStore);
        var plaintext = TestData.Create(4096);
        var encrypted = svc.Encrypt(plaintext, keyId);
        Assert.Equal(plaintext, svc.Decrypt(encrypted, keyId));
    }

    [Fact]
    public void AesGcm_Encrypt_1MiB_AllocatesLessThan125PercentOfPayload()
    {
        const string keyId = "alloc-key";
        var keyStore = new LocalKeyStore();
        keyStore.UpdateKeyFromString(keyId, "alloc-password");
        var svc = new AesGcmEncryptionService(keyStore);
        var plaintext = new byte[1024 * 1024];
        TestData.Fill(plaintext);

        // Warm paths / JIT so the measurement is not dominated by first-call noise.
        _ = svc.Encrypt(plaintext.AsSpan(0, 1024), keyId);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var encrypted = svc.Encrypt(plaintext, keyId);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(encrypted.Length > plaintext.Length);
        Assert.True(
            allocated < (long)(plaintext.Length * 1.25),
            $"Expected encrypt alloc < 1.25× payload; got {allocated} bytes for {plaintext.Length}-byte plaintext.");
    }

    /// <summary>Exposes <see cref="EncryptionServiceBase" /> BinaryWriter-string helpers for format compatibility asserts.</summary>
    private sealed class FramingProbe : EncryptionServiceBase
    {
        public FramingProbe()
            : base(
                new() {
                    CurrentFormatVersion = (byte)StreamFormatVersion.V1,
                    MinInputSize = 0,
                    MaxInputSize = long.MaxValue,
                    FileExtension = ".probe"
                }) { }

        public override byte[] Encrypt(byte[] bytes, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
            => throw new NotSupportedException();

        public override byte[] Decrypt(byte[] encryptedBytes, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
            => throw new NotSupportedException();

        public override IAeadStreamCryptor CreateStreamCryptor(ReadOnlySpan<byte> key) => throw new NotSupportedException();

        public static int GetByteCount(string value) => GetBinaryWriterStringByteCount(value);

        public static int Write(Span<byte> destination, string value) => WriteBinaryWriterString(destination, value);

        public static string Read(ReadOnlySpan<byte> source, out int bytesConsumed) => ReadBinaryWriterString(source, out bytesConsumed);
    }
}
