using System.Text;
using Lyo.Encryption;

namespace Lyo.Encryption.Tests;

public class EncryptionHeaderTests
{
    [Fact]
    public void Default_Values_AreCorrect()
    {
        var header = new EncryptionHeader();
        Assert.Equal((byte)StreamFormatVersion.V1, header.FormatVersion);
        Assert.Equal(0, header.DekAlgorithmId);
        Assert.Equal(0, header.KekAlgorithmId);
        Assert.Equal((byte)32, header.DekKeyMaterialBytes);
        Assert.Equal(string.Empty, header.KeyId);
        Assert.Equal(string.Empty, header.KeyVersion);
        Assert.Empty(header.EncryptedDataEncryptionKey);
    }

    [Fact]
    public void Read_FromBinaryReader_ReadsCorrectly()
    {
        var formatVersion = (byte)1;
        var dekAlgorithmId = (byte)0; // AES-GCM
        var kekAlgorithmId = (byte)0; // AES-GCM
        var keyId = "test-key-id";
        var keyVersion = "42";
        var encryptedDek = new byte[] { 1, 2, 3, 4, 5 };
        var buffer = new List<byte>();
        buffer.Add(formatVersion); // Format Version
        buffer.Add(dekAlgorithmId); // DEK Algorithm ID
        buffer.Add(kekAlgorithmId); // KEK Algorithm ID
        buffer.Add(32); // DekKeyMaterialBytes (AES-GCM)
        var keyIdBytes = Encoding.UTF8.GetBytes(keyId);
        buffer.AddRange(BitConverter.GetBytes(keyIdBytes.Length)); // KeyId length
        buffer.AddRange(keyIdBytes); // KeyId
        var keyVersionBytes = Encoding.UTF8.GetBytes(keyVersion);
        buffer.AddRange(BitConverter.GetBytes(keyVersionBytes.Length)); // KeyVersion length
        buffer.AddRange(keyVersionBytes); // KeyVersion
        buffer.AddRange(BitConverter.GetBytes(encryptedDek.Length)); // DEK length
        buffer.AddRange(encryptedDek); // Encrypted DEK
        using var stream = new MemoryStream(buffer.ToArray());
        using var reader = new BinaryReader(stream, Encoding.UTF8);
        var header = EncryptionHeader.Read(reader);
        Assert.Equal(formatVersion, header.FormatVersion);
        Assert.Equal(dekAlgorithmId, header.DekAlgorithmId);
        Assert.Equal(kekAlgorithmId, header.KekAlgorithmId);
        Assert.Equal(keyId, header.KeyId);
        Assert.Equal(keyVersion, header.KeyVersion);
        Assert.Equal(encryptedDek, header.EncryptedDataEncryptionKey);
    }

    [Fact]
    public void Read_FromStream_ReadsCorrectly()
    {
        var formatVersion = (byte)1;
        var dekAlgorithmId = (byte)1; // ChaCha20Poly1305
        var kekAlgorithmId = (byte)1; // ChaCha20Poly1305
        var keyId = "my-key";
        var keyVersion = "1";
        var encryptedDek = new byte[] { 10, 20, 30 };
        var buffer = new List<byte>();
        buffer.Add(formatVersion); // Format Version
        buffer.Add(dekAlgorithmId); // DEK Algorithm ID
        buffer.Add(kekAlgorithmId); // KEK Algorithm ID
        buffer.Add(32); // DekKeyMaterialBytes (ChaCha20-Poly1305)
        var keyIdBytes = Encoding.UTF8.GetBytes(keyId);
        buffer.AddRange(BitConverter.GetBytes(keyIdBytes.Length));
        buffer.AddRange(keyIdBytes);
        var keyVersionBytes = Encoding.UTF8.GetBytes(keyVersion);
        buffer.AddRange(BitConverter.GetBytes(keyVersionBytes.Length));
        buffer.AddRange(keyVersionBytes);
        buffer.AddRange(BitConverter.GetBytes(encryptedDek.Length));
        buffer.AddRange(encryptedDek);
        using var stream = new MemoryStream(buffer.ToArray());
        var header = EncryptionHeader.Read(stream);
        Assert.Equal(formatVersion, header.FormatVersion);
        Assert.Equal(dekAlgorithmId, header.DekAlgorithmId);
        Assert.Equal(kekAlgorithmId, header.KekAlgorithmId);
        Assert.Equal(keyId, header.KeyId);
        Assert.Equal(keyVersion, header.KeyVersion);
        Assert.Equal(encryptedDek, header.EncryptedDataEncryptionKey);
    }

    [Fact]
    public void Read_FromByteArray_ReadsCorrectly()
    {
        var formatVersion = (byte)1;
        var dekAlgorithmId = (byte)0;
        var kekAlgorithmId = (byte)0;
        var keyId = "test";
        var encryptedDek = new byte[] { 100, 200 };
        var buffer = new List<byte>();
        buffer.Add(formatVersion); // Format Version
        buffer.Add(dekAlgorithmId); // DEK Algorithm ID
        buffer.Add(kekAlgorithmId); // KEK Algorithm ID
        buffer.Add(32); // DekKeyMaterialBytes (AES-GCM)
        var keyIdBytes = Encoding.UTF8.GetBytes(keyId);
        buffer.AddRange(BitConverter.GetBytes(keyIdBytes.Length));
        buffer.AddRange(keyIdBytes);
        var keyVersionBytes = Encoding.UTF8.GetBytes("5");
        buffer.AddRange(BitConverter.GetBytes(keyVersionBytes.Length)); // KeyVersion length
        buffer.AddRange(keyVersionBytes); // KeyVersion
        buffer.AddRange(BitConverter.GetBytes(encryptedDek.Length));
        buffer.AddRange(encryptedDek);
        var header = EncryptionHeader.Read(buffer.ToArray());
        Assert.Equal(formatVersion, header.FormatVersion);
        Assert.Equal(dekAlgorithmId, header.DekAlgorithmId);
        Assert.Equal(kekAlgorithmId, header.KekAlgorithmId);
        Assert.Equal(keyId, header.KeyId);
        Assert.Equal("5", header.KeyVersion);
        Assert.Equal(encryptedDek, header.EncryptedDataEncryptionKey);
    }

    [Fact]
    public void Read_UnknownVersion_Throws()
    {
        var buffer = new List<byte>();
        buffer.Add(99); // Unsupported version
        buffer.Add(0); // DEK Algorithm ID
        buffer.Add(0); // KEK Algorithm ID
        buffer.Add(32); // DekKeyMaterialBytes
        buffer.AddRange(BitConverter.GetBytes(0)); // Empty KeyId
        buffer.AddRange(BitConverter.GetBytes(0)); // KeyVersion length (empty)
        buffer.AddRange(BitConverter.GetBytes(0)); // Empty DEK
        using var stream = new MemoryStream(buffer.ToArray());
        Assert.Throws<InvalidDataException>(() => EncryptionHeader.Read(stream));
    }

    [Fact]
    public void Write_ToBinaryWriter_WritesCorrectly()
    {
        var header = new EncryptionHeader {
            FormatVersion = 1,
            DekAlgorithmId = 0,
            KekAlgorithmId = 0,
            KeyId = "test-key",
            KeyVersion = "10",
            EncryptedDataEncryptionKey = new byte[] { 1, 2, 3 }
        };

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8);
        header.Write(writer);
        writer.Flush();
        stream.Position = 0;
        var readHeader = EncryptionHeader.Read(stream);
        Assert.Equal(header.FormatVersion, readHeader.FormatVersion);
        Assert.Equal(header.DekAlgorithmId, readHeader.DekAlgorithmId);
        Assert.Equal(header.KekAlgorithmId, readHeader.KekAlgorithmId);
        Assert.Equal(header.DekKeyMaterialBytes, readHeader.DekKeyMaterialBytes);
        Assert.Equal(header.KeyId, readHeader.KeyId);
        Assert.Equal(header.KeyVersion, readHeader.KeyVersion);
        Assert.Equal(header.EncryptedDataEncryptionKey, readHeader.EncryptedDataEncryptionKey);
    }

    [Fact]
    public void Write_ToStream_WritesCorrectly()
    {
        var header = new EncryptionHeader {
            FormatVersion = 1,
            DekAlgorithmId = 1,
            KekAlgorithmId = 1,
            KeyId = "stream-key",
            KeyVersion = "2",
            EncryptedDataEncryptionKey = new byte[] { 5, 6, 7, 8 }
        };

        using var stream = new MemoryStream();
        header.Write(stream);
        stream.Position = 0;
        var readHeader = EncryptionHeader.Read(stream);
        Assert.Equal(header.DekAlgorithmId, readHeader.DekAlgorithmId);
        Assert.Equal(header.KekAlgorithmId, readHeader.KekAlgorithmId);
        Assert.Equal(header.KeyId, readHeader.KeyId);
        Assert.Equal(header.KeyVersion, readHeader.KeyVersion);
        Assert.Equal(header.EncryptedDataEncryptionKey, readHeader.EncryptedDataEncryptionKey);
    }

    [Fact]
    public void Write_ToList_WritesCorrectly()
    {
        var header = new EncryptionHeader {
            FormatVersion = 1,
            DekAlgorithmId = 0,
            KekAlgorithmId = 0,
            KeyId = "list-key",
            KeyVersion = "3",
            EncryptedDataEncryptionKey = new byte[] { 9, 10, 11 }
        };

        var buffer = new List<byte>();
        header.Write(buffer);
        var readHeader = EncryptionHeader.Read(buffer.ToArray());
        Assert.Equal(header.DekAlgorithmId, readHeader.DekAlgorithmId);
        Assert.Equal(header.KekAlgorithmId, readHeader.KekAlgorithmId);
        Assert.Equal(header.KeyId, readHeader.KeyId);
        Assert.Equal(header.KeyVersion, readHeader.KeyVersion);
        Assert.Equal(header.EncryptedDataEncryptionKey, readHeader.EncryptedDataEncryptionKey);
    }

    [Fact]
    public void ReadWrite_Roundtrip_PreservesData()
    {
        var originalHeader = new EncryptionHeader {
            FormatVersion = 1,
            DekAlgorithmId = 0,
            KekAlgorithmId = 0,
            KeyId = "roundtrip-key",
            KeyVersion = "15",
            EncryptedDataEncryptionKey = new byte[] { 20, 21, 22, 23, 24 }
        };

        using var stream = new MemoryStream();
        originalHeader.Write(stream);
        stream.Position = 0;
        var readHeader = EncryptionHeader.Read(stream);
        Assert.Equal(originalHeader.FormatVersion, readHeader.FormatVersion);
        Assert.Equal(originalHeader.DekAlgorithmId, readHeader.DekAlgorithmId);
        Assert.Equal(originalHeader.KekAlgorithmId, readHeader.KekAlgorithmId);
        Assert.Equal(originalHeader.KeyId, readHeader.KeyId);
        Assert.Equal(originalHeader.KeyVersion, readHeader.KeyVersion);
        Assert.Equal(originalHeader.EncryptedDataEncryptionKey, readHeader.EncryptedDataEncryptionKey);
    }

    [Fact]
    public void GetHeaderSize_CalculatesCorrectly()
    {
        var header = new EncryptionHeader {
            FormatVersion = 1,
            DekAlgorithmId = 0,
            KekAlgorithmId = 0,
            KeyId = "test", // 4 bytes
            KeyVersion = "1", // 1 byte
            EncryptedDataEncryptionKey = new byte[] { 1, 2, 3 } // 3 bytes
        };

        // Expected: 1 + 1 + 1 + 1 (DekKeyMaterialBytes) + 4 + 4 + 4 + 1 + 4 + 3
        var expectedSize = 1 + 1 + 1 + 1 + 4 + 4 + 4 + 1 + 4 + 3;
        Assert.Equal(expectedSize, header.GetHeaderSize());
    }

    [Fact]
    public void GetHeaderSize_EmptyKeyId_CalculatesCorrectly()
    {
        var header = new EncryptionHeader {
            FormatVersion = 1,
            DekAlgorithmId = 0,
            KekAlgorithmId = 0,
            KeyId = string.Empty,
            EncryptedDataEncryptionKey = new byte[] { 1, 2 }
        };

        // Expected: 1 + 1 + 1 + 1 (DekKeyMaterialBytes) + 4 + 0 + 4 + 0 + 4 + 2
        var expectedSize = 1 + 1 + 1 + 1 + 4 + 0 + 4 + 0 + 4 + 2;
        Assert.Equal(expectedSize, header.GetHeaderSize());
    }

    [Fact]
    public void With_UpdatesKeyId()
    {
        var original = new EncryptionHeader {
            FormatVersion = 1,
            DekAlgorithmId = 0,
            KekAlgorithmId = 0,
            KeyId = "original",
            KeyVersion = "1",
            EncryptedDataEncryptionKey = new byte[] { 1 }
        };

        var updated = original.With("updated");
        Assert.Equal("updated", updated.KeyId);
        Assert.Equal(original.DekAlgorithmId, updated.DekAlgorithmId);
        Assert.Equal(original.KekAlgorithmId, updated.KekAlgorithmId);
        Assert.Equal(original.KeyVersion, updated.KeyVersion);
        Assert.Equal(original.EncryptedDataEncryptionKey, updated.EncryptedDataEncryptionKey);
    }

    [Fact]
    public void With_UpdatesKeyVersion()
    {
        var original = new EncryptionHeader {
            FormatVersion = 1,
            DekAlgorithmId = 0,
            KekAlgorithmId = 0,
            KeyId = "test",
            KeyVersion = "1",
            EncryptedDataEncryptionKey = new byte[] { 1 }
        };

        var updated = original.With(keyVersion: "5");
        Assert.Equal(original.KeyId, updated.KeyId);
        Assert.Equal("5", updated.KeyVersion);
        Assert.Equal(original.EncryptedDataEncryptionKey, updated.EncryptedDataEncryptionKey);
    }

    [Fact]
    public void With_UpdatesEncryptedDek()
    {
        var original = new EncryptionHeader {
            FormatVersion = 1,
            DekAlgorithmId = 0,
            KekAlgorithmId = 0,
            KeyId = "test",
            KeyVersion = "1",
            EncryptedDataEncryptionKey = new byte[] { 1, 2 }
        };

        var newDek = new byte[] { 10, 20, 30 };
        var updated = original.With(encryptedDataEncryptionKey: newDek);
        Assert.Equal(original.KeyId, updated.KeyId);
        Assert.Equal(original.KeyVersion, updated.KeyVersion);
        Assert.Equal(newDek, updated.EncryptedDataEncryptionKey);
    }

    [Fact]
    public void With_UpdatesFormatVersion()
    {
        var original = new EncryptionHeader {
            FormatVersion = 1,
            DekAlgorithmId = 0,
            KekAlgorithmId = 0,
            KeyId = "test"
        };

        var updated = original.With(formatVersion: 2);
        Assert.Equal(2, updated.FormatVersion);
        Assert.Equal(original.KeyId, updated.KeyId);
    }

    [Fact]
    public void With_UpdatesMultipleFields()
    {
        var original = new EncryptionHeader {
            FormatVersion = 1,
            DekAlgorithmId = 0,
            KekAlgorithmId = 0,
            KeyId = "original",
            KeyVersion = "1",
            EncryptedDataEncryptionKey = new byte[] { 1 }
        };

        var newDek = new byte[] { 99 };
        var updated = original.With("new-key", "10", newDek);
        Assert.Equal("new-key", updated.KeyId);
        Assert.Equal("10", updated.KeyVersion);
        Assert.Equal(newDek, updated.EncryptedDataEncryptionKey);
        Assert.Equal(original.DekAlgorithmId, updated.DekAlgorithmId);
    }

    [Fact]
    public void With_NullParameters_KeepsOriginalValues()
    {
        var original = new EncryptionHeader {
            FormatVersion = 1,
            DekAlgorithmId = 0,
            KekAlgorithmId = 0,
            KeyId = "test",
            KeyVersion = "5",
            EncryptedDataEncryptionKey = new byte[] { 1, 2, 3 }
        };

        var updated = original.With();
        Assert.Equal(original.KeyId, updated.KeyId);
        Assert.Equal(original.KeyVersion, updated.KeyVersion);
        Assert.Equal(original.EncryptedDataEncryptionKey, updated.EncryptedDataEncryptionKey);
    }

    [Fact]
    public void Read_EmptyKeyId_HandlesCorrectly()
    {
        var buffer = new List<byte>();
        buffer.Add(1); // Format Version
        buffer.Add(0); // DEK Algorithm ID
        buffer.Add(0); // KEK Algorithm ID
        buffer.Add(32); // DekKeyMaterialBytes (AES-GCM)
        buffer.AddRange(BitConverter.GetBytes(0)); // Empty KeyId
        var keyVersionBytes = Encoding.UTF8.GetBytes("1");
        buffer.AddRange(BitConverter.GetBytes(keyVersionBytes.Length)); // KeyVersion length
        buffer.AddRange(keyVersionBytes); // KeyVersion
        buffer.AddRange(BitConverter.GetBytes(2)); // DEK length
        buffer.AddRange(new byte[] { 1, 2 }); // DEK
        var header = EncryptionHeader.Read(buffer.ToArray());
        Assert.Equal(string.Empty, header.KeyId);
        Assert.Equal("1", header.KeyVersion);
        Assert.Equal(new byte[] { 1, 2 }, header.EncryptedDataEncryptionKey);
    }

    [Fact]
    public void Write_EmptyKeyId_HandlesCorrectly()
    {
        var header = new EncryptionHeader {
            FormatVersion = 1,
            DekAlgorithmId = 0,
            KekAlgorithmId = 0,
            KeyId = string.Empty,
            EncryptedDataEncryptionKey = new byte[] { 5 }
        };

        var buffer = new List<byte>();
        header.Write(buffer);
        var readHeader = EncryptionHeader.Read(buffer.ToArray());
        Assert.Equal(string.Empty, readHeader.KeyId);
        Assert.Equal(header.EncryptedDataEncryptionKey, readHeader.EncryptedDataEncryptionKey);
    }
}