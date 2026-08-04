using Lyo.Encryption.Security;
using Lyo.Encryption.Streaming;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Lyo.Encryption.AesCcm;

/// <summary>
/// Per-stream AES-CCM cipher reused across every chunk of a streaming operation (see <see cref="IAeadStreamCryptor" />). Each chunk is encrypted independently with the
/// stream-supplied nonce (BouncyCastle on every target framework, matching the single-shot wire format). The cipher object and grow-once scratch buffers are reused per chunk.
/// </summary>
internal sealed class AesCcmStreamCryptor(ReadOnlySpan<byte> key) : IAeadStreamCryptor
{
    private readonly CcmBlockCipher _cipher = new(new AesEngine());
    private readonly KeyParameter _key = new(key.ToArray());
    private readonly byte[] _nonceBuffer = new byte[AesCcmHelper.NonceSize];
    private byte[] _inBuffer = [];

    public int NonceSize => AesCcmHelper.NonceSize;

    public int TagSize => AesCcmHelper.TagSize;

    public void Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> nonce, Span<byte> ciphertextAndTag, ReadOnlySpan<byte> associatedData = default)
    {
        AesCcmHelper.ValidatePlaintextLength(plaintext.Length, nameof(plaintext));
        nonce.CopyTo(_nonceBuffer);
        _cipher.Init(true, new AeadParameters(_key, AesCcmHelper.TagSize * 8, _nonceBuffer, associatedData.IsEmpty ? null : associatedData.ToArray()));
        EnsureBuffer(plaintext.Length);
        plaintext.CopyTo(_inBuffer);
        // CCM is a packet mode in BouncyCastle: ProcessPacket performs the whole operation and returns [ciphertext][tag].
        var packed = _cipher.ProcessPacket(_inBuffer, 0, plaintext.Length);
        packed.AsSpan().CopyTo(ciphertextAndTag);
    }

    public void Decrypt(ReadOnlySpan<byte> ciphertextAndTag, ReadOnlySpan<byte> nonce, Span<byte> plaintext, ReadOnlySpan<byte> associatedData = default)
    {
        nonce.CopyTo(_nonceBuffer);
        _cipher.Init(false, new AeadParameters(_key, AesCcmHelper.TagSize * 8, _nonceBuffer, associatedData.IsEmpty ? null : associatedData.ToArray()));
        EnsureBuffer(ciphertextAndTag.Length);
        ciphertextAndTag.CopyTo(_inBuffer);
        var unpacked = _cipher.ProcessPacket(_inBuffer, 0, ciphertextAndTag.Length);
        unpacked.AsSpan().CopyTo(plaintext);
        SecurityUtilities.Clear(unpacked);
    }

    public void Dispose() => SecurityUtilities.Clear(_inBuffer);

    private void EnsureBuffer(int size)
    {
        if (_inBuffer.Length < size)
            _inBuffer = new byte[size];
    }
}