using Lyo.Encryption.Streaming;
#if !NET10_0_OR_GREATER
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
#endif

namespace Lyo.Encryption.AesGcm;

/// <summary>Per-stream AES-GCM cipher reused across every chunk of a streaming operation (see <see cref="IAeadStreamCryptor" />).</summary>
internal sealed class AesGcmStreamCryptor : IAeadStreamCryptor
{
    public int NonceSize => AesGcmHelper.NonceSize;

    public int TagSize => AesGcmHelper.TagSize;

#if NET10_0_OR_GREATER
    private readonly System.Security.Cryptography.AesGcm _aes;

    public AesGcmStreamCryptor(ReadOnlySpan<byte> key) => _aes = new System.Security.Cryptography.AesGcm(key, AesGcmHelper.TagSize);

    public void Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> nonce, Span<byte> ciphertextAndTag)
        => _aes.Encrypt(nonce, plaintext, ciphertextAndTag[..plaintext.Length], ciphertextAndTag.Slice(plaintext.Length, AesGcmHelper.TagSize));

    public void Decrypt(ReadOnlySpan<byte> ciphertextAndTag, ReadOnlySpan<byte> nonce, Span<byte> plaintext)
        => _aes.Decrypt(nonce, ciphertextAndTag[..^AesGcmHelper.TagSize], ciphertextAndTag[^AesGcmHelper.TagSize..], plaintext);

    public void Dispose() => _aes.Dispose();
#else
    // Reuse one cipher + key schedule and grow-once scratch buffers for the whole stream; only Init (cheap) runs per chunk.
    private readonly KeyParameter _key;
    private readonly GcmBlockCipher _cipher = new(new AesEngine());
    private readonly byte[] _nonceBuffer = new byte[AesGcmHelper.NonceSize];
    private byte[] _inBuffer = [];
    private byte[] _outBuffer = [];

    public AesGcmStreamCryptor(ReadOnlySpan<byte> key) => _key = new(key.ToArray());

    public void Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> nonce, Span<byte> ciphertextAndTag)
    {
        nonce.CopyTo(_nonceBuffer);
        _cipher.Init(true, new AeadParameters(_key, AesGcmHelper.TagSize * 8, _nonceBuffer, null));
        EnsureBuffers(plaintext.Length, _cipher.GetOutputSize(plaintext.Length));
        plaintext.CopyTo(_inBuffer);
        var written = _cipher.ProcessBytes(_inBuffer, 0, plaintext.Length, _outBuffer, 0);
        written += _cipher.DoFinal(_outBuffer, written);
        _outBuffer.AsSpan(0, written).CopyTo(ciphertextAndTag);
    }

    public void Decrypt(ReadOnlySpan<byte> ciphertextAndTag, ReadOnlySpan<byte> nonce, Span<byte> plaintext)
    {
        nonce.CopyTo(_nonceBuffer);
        _cipher.Init(false, new AeadParameters(_key, AesGcmHelper.TagSize * 8, _nonceBuffer, null));
        EnsureBuffers(ciphertextAndTag.Length, _cipher.GetOutputSize(ciphertextAndTag.Length));
        ciphertextAndTag.CopyTo(_inBuffer);
        var written = _cipher.ProcessBytes(_inBuffer, 0, ciphertextAndTag.Length, _outBuffer, 0);
        written += _cipher.DoFinal(_outBuffer, written);
        _outBuffer.AsSpan(0, written).CopyTo(plaintext);
    }

    private void EnsureBuffers(int inSize, int outSize)
    {
        if (_inBuffer.Length < inSize)
            _inBuffer = new byte[inSize];

        if (_outBuffer.Length < outSize)
            _outBuffer = new byte[outSize];
    }

    public void Dispose() { }
#endif
}