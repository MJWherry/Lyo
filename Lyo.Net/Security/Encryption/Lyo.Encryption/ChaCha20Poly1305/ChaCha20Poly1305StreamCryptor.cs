using Lyo.Encryption.Streaming;
#if !NET10_0_OR_GREATER
using Org.BouncyCastle.Crypto.Parameters;
#endif

namespace Lyo.Encryption.ChaCha20Poly1305;

/// <summary>Per-stream ChaCha20-Poly1305 cipher reused across every chunk of a streaming operation (see <see cref="IAeadStreamCryptor" />).</summary>
internal sealed class ChaCha20Poly1305StreamCryptor : IAeadStreamCryptor
{
    public int NonceSize => ChaCha20Poly1305Helper.NonceSize;

    public int TagSize => ChaCha20Poly1305Helper.TagSize;

#if NET10_0_OR_GREATER
    private readonly System.Security.Cryptography.ChaCha20Poly1305 _chacha;

    public ChaCha20Poly1305StreamCryptor(ReadOnlySpan<byte> key) => _chacha = new(key);

    public void Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> nonce, Span<byte> ciphertextAndTag, ReadOnlySpan<byte> associatedData = default)
        => _chacha.Encrypt(nonce, plaintext, ciphertextAndTag[..plaintext.Length], ciphertextAndTag.Slice(plaintext.Length, ChaCha20Poly1305Helper.TagSize), associatedData);

    public void Decrypt(ReadOnlySpan<byte> ciphertextAndTag, ReadOnlySpan<byte> nonce, Span<byte> plaintext, ReadOnlySpan<byte> associatedData = default)
        => _chacha.Decrypt(nonce, ciphertextAndTag[..^ChaCha20Poly1305Helper.TagSize], ciphertextAndTag[^ChaCha20Poly1305Helper.TagSize..], plaintext, associatedData);

    public void Dispose() => _chacha.Dispose();
#else
    // Reuse one cipher + key schedule and grow-once scratch buffers for the whole stream; only Init (cheap) runs per chunk.
    private readonly KeyParameter _key;
    private readonly Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305 _cipher = new();
    private readonly byte[] _nonceBuffer = new byte[ChaCha20Poly1305Helper.NonceSize];
    private byte[] _inBuffer = [];
    private byte[] _outBuffer = [];

    public ChaCha20Poly1305StreamCryptor(ReadOnlySpan<byte> key) => _key = new(key.ToArray());

    public void Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> nonce, Span<byte> ciphertextAndTag, ReadOnlySpan<byte> associatedData = default)
    {
        nonce.CopyTo(_nonceBuffer);
        _cipher.Init(true, new AeadParameters(_key, ChaCha20Poly1305Helper.TagSize * 8, _nonceBuffer, associatedData.IsEmpty ? null : associatedData.ToArray()));
        EnsureBuffers(plaintext.Length, _cipher.GetOutputSize(plaintext.Length));
        plaintext.CopyTo(_inBuffer);
        var written = _cipher.ProcessBytes(_inBuffer, 0, plaintext.Length, _outBuffer, 0);
        written += _cipher.DoFinal(_outBuffer, written);
        _outBuffer.AsSpan(0, written).CopyTo(ciphertextAndTag);
    }

    public void Decrypt(ReadOnlySpan<byte> ciphertextAndTag, ReadOnlySpan<byte> nonce, Span<byte> plaintext, ReadOnlySpan<byte> associatedData = default)
    {
        nonce.CopyTo(_nonceBuffer);
        _cipher.Init(false, new AeadParameters(_key, ChaCha20Poly1305Helper.TagSize * 8, _nonceBuffer, associatedData.IsEmpty ? null : associatedData.ToArray()));
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