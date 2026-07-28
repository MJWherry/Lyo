using Lyo.Encryption.Security;
using Lyo.Encryption.Streaming;
using Org.BouncyCastle.Crypto.Parameters;

namespace Lyo.Encryption.XChaCha20Poly1305;

/// <summary>
/// Per-stream XChaCha20-Poly1305 cipher reused across every chunk of a streaming operation (see <see cref="IAeadStreamCryptor" />). XChaCha20 derives a subkey from the first
/// 16 nonce bytes via HChaCha20; within a stream those bytes come from the constant per-stream nonce prefix, so the subkey is computed once and cached (recomputed only if the prefix
/// changes). The BouncyCastle cipher and grow-once scratch buffers are reused for every chunk, so there is no per-chunk heap allocation.
/// </summary>
internal sealed class XChaCha20Poly1305StreamCryptor : IAeadStreamCryptor
{
    private readonly Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305 _cipher = new();
    private readonly byte[] _innerNonce = new byte[12];
    private readonly byte[] _key;
    private readonly byte[] _subkey = new byte[32];
    private readonly byte[] _subkeyNonce16 = new byte[16];
    private byte[] _inBuffer = [];
    private byte[] _outBuffer = [];
    private KeyParameter? _subkeyParameter;

    public XChaCha20Poly1305StreamCryptor(ReadOnlySpan<byte> key) => _key = key.ToArray();

    public int NonceSize => XChaCha20Poly1305Helper.NonceSize;

    public int TagSize => XChaCha20Poly1305Helper.TagSize;

    public void Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> nonce, Span<byte> ciphertextAndTag, ReadOnlySpan<byte> associatedData = default)
    {
        PrepareSubkeyAndNonce(nonce);
        _cipher.Init(true, new AeadParameters(_subkeyParameter, XChaCha20Poly1305Helper.TagSize * 8, _innerNonce, associatedData.IsEmpty ? null : associatedData.ToArray()));
        EnsureBuffers(plaintext.Length, _cipher.GetOutputSize(plaintext.Length));
        plaintext.CopyTo(_inBuffer);
        var written = _cipher.ProcessBytes(_inBuffer, 0, plaintext.Length, _outBuffer, 0);
        written += _cipher.DoFinal(_outBuffer, written);
        _outBuffer.AsSpan(0, written).CopyTo(ciphertextAndTag);
    }

    public void Decrypt(ReadOnlySpan<byte> ciphertextAndTag, ReadOnlySpan<byte> nonce, Span<byte> plaintext, ReadOnlySpan<byte> associatedData = default)
    {
        PrepareSubkeyAndNonce(nonce);
        _cipher.Init(false, new AeadParameters(_subkeyParameter, XChaCha20Poly1305Helper.TagSize * 8, _innerNonce, associatedData.IsEmpty ? null : associatedData.ToArray()));
        EnsureBuffers(ciphertextAndTag.Length, _cipher.GetOutputSize(ciphertextAndTag.Length));
        ciphertextAndTag.CopyTo(_inBuffer);
        var written = _cipher.ProcessBytes(_inBuffer, 0, ciphertextAndTag.Length, _outBuffer, 0);
        written += _cipher.DoFinal(_outBuffer, written);
        _outBuffer.AsSpan(0, written).CopyTo(plaintext);
    }

    public void Dispose()
    {
        SecurityUtilities.Clear(_key);
        SecurityUtilities.Clear(_subkey);
        SecurityUtilities.Clear(_inBuffer);
        SecurityUtilities.Clear(_outBuffer);
    }

    /// <summary>Derives (or reuses) the HChaCha20 subkey for <paramref name="nonce24" /> and fills the 12-byte inner IETF nonce (4 zero bytes + nonce bytes 16..24).</summary>
    private void PrepareSubkeyAndNonce(ReadOnlySpan<byte> nonce24)
    {
        var first16 = nonce24[..16];
        if (_subkeyParameter == null || !first16.SequenceEqual(_subkeyNonce16)) {
            HChaCha20.Block(_key, first16, _subkey);
            first16.CopyTo(_subkeyNonce16);
            _subkeyParameter = new(_subkey);
        }

        _innerNonce[0] = 0;
        _innerNonce[1] = 0;
        _innerNonce[2] = 0;
        _innerNonce[3] = 0;
        nonce24.Slice(16, 8).CopyTo(_innerNonce.AsSpan(4));
    }

    private void EnsureBuffers(int inSize, int outSize)
    {
        if (_inBuffer.Length < inSize)
            _inBuffer = new byte[inSize];

        if (_outBuffer.Length < outSize)
            _outBuffer = new byte[outSize];
    }
}