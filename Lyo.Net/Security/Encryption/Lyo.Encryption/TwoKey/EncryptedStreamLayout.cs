using Lyo.Encryption.Security;

namespace Lyo.Encryption.TwoKey;

/// <summary>Parsed two-key stream prefix: header bytes, the unwrapped DEK, and the fixed frame size of every non-final chunk. Dispose wipes the DEK.</summary>
public sealed class EncryptedStreamLayout : IDisposable
{
    private readonly byte[] _dek;
    private bool _disposed;

    /// <summary>Bytes before the first chunk length prefix, including the nonce prefix.</summary>
    public int HeaderLength { get; }

    /// <summary>Plaintext bytes in each non-final chunk. A one-chunk stream uses that chunk's plaintext length.</summary>
    public int ChunkSize { get; }

    /// <summary>On-disk bytes of one full chunk: length prefix, ciphertext, and tag.</summary>
    public int FrameSize { get; }

    /// <summary>Header bytes, including the nonce prefix. Same instance for the life of this layout.</summary>
    public byte[] Header { get; }

    /// <summary>Unwrapped data-encryption key. Cleared when this layout is disposed.</summary>
    public byte[] Dek => _disposed ? throw new ObjectDisposedException(nameof(EncryptedStreamLayout)) : _dek;

    /// <summary>Builds a layout. <paramref name="dek" /> is owned by this instance and wiped on dispose.</summary>
    public EncryptedStreamLayout(int headerLength, int chunkSize, int frameSize, byte[] header, byte[] dek)
    {
        HeaderLength = headerLength;
        ChunkSize = chunkSize;
        FrameSize = frameSize;
        Header = header;
        _dek = dek;
    }

    /// <summary>Wipes the DEK.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        SecurityUtilities.Clear(_dek);
    }
}