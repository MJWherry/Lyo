using System.Buffers;
using System.Buffers.Binary;
using Lyo.Exceptions;
using Lyo.Hashing.Internal;

#if NET5_0_OR_GREATER
using System.IO.Hashing;
#endif

namespace Lyo.Hashing;

/// <summary>
/// Static non-cryptographic checksum helpers (CRC-32, CRC-32C, CRC-64/ECMA-182, Adler-32). These detect accidental corruption only and are <strong>not</strong> a security
/// boundary — see the package README. On modern .NET the CRC-32 and CRC-64 buffer paths delegate to <c>System.IO.Hashing</c>; everything else uses internal table-driven
/// implementations that produce identical results across targets.
/// </summary>
public static class Checksummer
{
    /// <summary>Computes <paramref name="algorithm" /> over <paramref name="data" /> and returns the result as big-endian bytes (4 bytes for 32-bit checksums, 8 for CRC-64).</summary>
    public static byte[] Compute(ChecksumAlgorithm algorithm, ReadOnlySpan<byte> data) => ToBigEndianBytes(ComputeValue(algorithm, data), OutputSize(algorithm));

    /// <inheritdoc cref="Compute(ChecksumAlgorithm, ReadOnlySpan{byte})" />
    public static byte[] Compute(ChecksumAlgorithm algorithm, byte[] data)
    {
        ArgumentHelpers.ThrowIfNull(data);
        return Compute(algorithm, data.AsSpan());
    }

    /// <summary>Computes <paramref name="algorithm" /> over <paramref name="stream" /> (current position through end-of-stream). Does not dispose <paramref name="stream" />.</summary>
    public static byte[] Compute(ChecksumAlgorithm algorithm, Stream stream) => ToBigEndianBytes(ComputeValue(algorithm, stream), OutputSize(algorithm));

    /// <summary>Computes <paramref name="algorithm" /> over <paramref name="data" /> and returns the raw numeric value (32-bit results occupy the low bits).</summary>
    public static ulong ComputeValue(ChecksumAlgorithm algorithm, ReadOnlySpan<byte> data)
    {
#if NET5_0_OR_GREATER
        switch (algorithm) {
            case ChecksumAlgorithm.Crc32:
                return Crc32.HashToUInt32(data);
            case ChecksumAlgorithm.Crc64:
                return Crc64.HashToUInt64(data);
        }
#endif
        var calculator = ChecksumCalculator.Create(algorithm);
        calculator.Append(data);
        return calculator.GetCurrentValue();
    }

    /// <inheritdoc cref="ComputeValue(ChecksumAlgorithm, ReadOnlySpan{byte})" />
    public static ulong ComputeValue(ChecksumAlgorithm algorithm, byte[] data)
    {
        ArgumentHelpers.ThrowIfNull(data);
        return ComputeValue(algorithm, data.AsSpan());
    }

    /// <summary>Computes <paramref name="algorithm" /> over <paramref name="stream" /> and returns the raw numeric value. Does not dispose <paramref name="stream" />.</summary>
    public static ulong ComputeValue(ChecksumAlgorithm algorithm, Stream stream)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        OperationHelpers.ThrowIfNotReadable(stream);
        var calculator = ChecksumCalculator.Create(algorithm);
        var buffer = ArrayPool<byte>.Shared.Rent(8192);
        try {
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                calculator.Append(buffer.AsSpan(0, read));
        }
        finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return calculator.GetCurrentValue();
    }

    /// <summary>CRC-32 (IEEE/ISO-HDLC) numeric value of <paramref name="data" />.</summary>
    public static uint ComputeCrc32(ReadOnlySpan<byte> data) => (uint)ComputeValue(ChecksumAlgorithm.Crc32, data);

    /// <summary>CRC-32C (Castagnoli) numeric value of <paramref name="data" />.</summary>
    public static uint ComputeCrc32C(ReadOnlySpan<byte> data) => (uint)ComputeValue(ChecksumAlgorithm.Crc32C, data);

    /// <summary>CRC-64/ECMA-182 numeric value of <paramref name="data" />.</summary>
    public static ulong ComputeCrc64(ReadOnlySpan<byte> data) => ComputeValue(ChecksumAlgorithm.Crc64, data);

    /// <summary>Adler-32 numeric value of <paramref name="data" />.</summary>
    public static uint ComputeAdler32(ReadOnlySpan<byte> data) => (uint)ComputeValue(ChecksumAlgorithm.Adler32, data);

    internal static int OutputSize(ChecksumAlgorithm algorithm)
        => algorithm switch {
            ChecksumAlgorithm.Crc32 => 4,
            ChecksumAlgorithm.Crc32C => 4,
            ChecksumAlgorithm.Crc64 => 8,
            ChecksumAlgorithm.Adler32 => 4,
            var _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
        };

    internal static byte[] ToBigEndianBytes(ulong value, int size)
    {
        var result = new byte[size];
        if (size == 4)
            BinaryPrimitives.WriteUInt32BigEndian(result, (uint)value);
        else
            BinaryPrimitives.WriteUInt64BigEndian(result, value);

        return result;
    }
}