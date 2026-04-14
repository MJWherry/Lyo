using System.Buffers.Binary;

namespace Lyo.Encryption.Symmetric.ChaCha.XChaCha20Poly1305;

/// <summary>HChaCha20 per RFC 8439 §2.3 (used by XChaCha20-Poly1305 subkey derivation).</summary>
internal static class HChaCha20
{
    public static void Block(ReadOnlySpan<byte> key32, ReadOnlySpan<byte> nonce16, Span<byte> out32)
    {
        if (key32.Length != 32 || nonce16.Length != 16 || out32.Length != 32)
            throw new ArgumentException("HChaCha20 requires a 32-byte key, 16-byte nonce, and 32-byte output.");

        Span<uint> st = stackalloc uint[16];
        st[0] = 0x61707865;
        st[1] = 0x3320646e;
        st[2] = 0x79622d32;
        st[3] = 0x6b206574;
        st[4] = BinaryPrimitives.ReadUInt32LittleEndian(key32[..4]);
        st[5] = BinaryPrimitives.ReadUInt32LittleEndian(key32.Slice(4, 4));
        st[6] = BinaryPrimitives.ReadUInt32LittleEndian(key32.Slice(8, 4));
        st[7] = BinaryPrimitives.ReadUInt32LittleEndian(key32.Slice(12, 4));
        st[8] = BinaryPrimitives.ReadUInt32LittleEndian(key32.Slice(16, 4));
        st[9] = BinaryPrimitives.ReadUInt32LittleEndian(key32.Slice(20, 4));
        st[10] = BinaryPrimitives.ReadUInt32LittleEndian(key32.Slice(24, 4));
        st[11] = BinaryPrimitives.ReadUInt32LittleEndian(key32.Slice(28, 4));
        st[12] = BinaryPrimitives.ReadUInt32LittleEndian(nonce16[..4]);
        st[13] = BinaryPrimitives.ReadUInt32LittleEndian(nonce16.Slice(4, 4));
        st[14] = BinaryPrimitives.ReadUInt32LittleEndian(nonce16.Slice(8, 4));
        st[15] = BinaryPrimitives.ReadUInt32LittleEndian(nonce16.Slice(12, 4));

        var x0 = st[0];
        var x1 = st[1];
        var x2 = st[2];
        var x3 = st[3];
        var x4 = st[4];
        var x5 = st[5];
        var x6 = st[6];
        var x7 = st[7];
        var x8 = st[8];
        var x9 = st[9];
        var x10 = st[10];
        var x11 = st[11];
        var x12 = st[12];
        var x13 = st[13];
        var x14 = st[14];
        var x15 = st[15];

        for (var r = 20; r > 0; r -= 2) {
            x0 += x4;
            x12 = R(x12 ^ x0, 16);
            x1 += x5;
            x13 = R(x13 ^ x1, 16);
            x2 += x6;
            x14 = R(x14 ^ x2, 16);
            x3 += x7;
            x15 = R(x15 ^ x3, 16);

            x8 += x12;
            x4 = R(x4 ^ x8, 12);
            x9 += x13;
            x5 = R(x5 ^ x9, 12);
            x10 += x14;
            x6 = R(x6 ^ x10, 12);
            x11 += x15;
            x7 = R(x7 ^ x11, 12);

            x0 += x4;
            x12 = R(x12 ^ x0, 8);
            x1 += x5;
            x13 = R(x13 ^ x1, 8);
            x2 += x6;
            x14 = R(x14 ^ x2, 8);
            x3 += x7;
            x15 = R(x15 ^ x3, 8);

            x8 += x12;
            x4 = R(x4 ^ x8, 7);
            x9 += x13;
            x5 = R(x5 ^ x9, 7);
            x10 += x14;
            x6 = R(x6 ^ x10, 7);
            x11 += x15;
            x7 = R(x7 ^ x11, 7);

            x0 += x5;
            x15 = R(x15 ^ x0, 16);
            x1 += x6;
            x12 = R(x12 ^ x1, 16);
            x2 += x7;
            x13 = R(x13 ^ x2, 16);
            x3 += x4;
            x14 = R(x14 ^ x3, 16);

            x10 += x15;
            x5 = R(x5 ^ x10, 12);
            x11 += x12;
            x6 = R(x6 ^ x11, 12);
            x8 += x13;
            x7 = R(x7 ^ x8, 12);
            x9 += x14;
            x4 = R(x4 ^ x9, 12);

            x0 += x5;
            x15 = R(x15 ^ x0, 8);
            x1 += x6;
            x12 = R(x12 ^ x1, 8);
            x2 += x7;
            x13 = R(x13 ^ x2, 8);
            x3 += x4;
            x14 = R(x14 ^ x3, 8);

            x10 += x15;
            x5 = R(x5 ^ x10, 7);
            x11 += x12;
            x6 = R(x6 ^ x11, 7);
            x8 += x13;
            x7 = R(x7 ^ x8, 7);
            x9 += x14;
            x4 = R(x4 ^ x9, 7);
        }

        BinaryPrimitives.WriteUInt32LittleEndian(out32[..4], x0 + st[0]);
        BinaryPrimitives.WriteUInt32LittleEndian(out32.Slice(4, 4), x1 + st[1]);
        BinaryPrimitives.WriteUInt32LittleEndian(out32.Slice(8, 4), x2 + st[2]);
        BinaryPrimitives.WriteUInt32LittleEndian(out32.Slice(12, 4), x3 + st[3]);
        BinaryPrimitives.WriteUInt32LittleEndian(out32.Slice(16, 4), x12 + st[12]);
        BinaryPrimitives.WriteUInt32LittleEndian(out32.Slice(20, 4), x13 + st[13]);
        BinaryPrimitives.WriteUInt32LittleEndian(out32.Slice(24, 4), x14 + st[14]);
        BinaryPrimitives.WriteUInt32LittleEndian(out32.Slice(28, 4), x15 + st[15]);
    }

    private static uint R(uint v, int c) => (v << c) | (v >> (32 - c));
}
