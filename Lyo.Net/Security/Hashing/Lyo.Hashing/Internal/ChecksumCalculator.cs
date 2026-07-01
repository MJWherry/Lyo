namespace Lyo.Hashing.Internal;

/// <summary>
/// Incremental, allocation-free state for a single <see cref="ChecksumAlgorithm" />. One instance accumulates bytes via <see cref="Append" />; <see cref="GetCurrentValue" />
/// is non-mutating so it can be read repeatedly while more data is appended. Implementations are the cross-target source of truth (used directly on netstandard2.0, for streaming on
/// all targets, and for the algorithms not provided by <c>System.IO.Hashing</c>).
/// </summary>
internal abstract class ChecksumCalculator
{
    /// <summary>Number of bytes in the big-endian output (4 for 32-bit checksums, 8 for CRC-64).</summary>
    public abstract int HashSizeInBytes { get; }

    /// <summary>Folds <paramref name="data" /> into the running checksum state.</summary>
    public abstract void Append(ReadOnlySpan<byte> data);

    /// <summary>Returns the checksum of all data appended so far without mutating the state. 32-bit results occupy the low bits.</summary>
    public abstract ulong GetCurrentValue();

    public static ChecksumCalculator Create(ChecksumAlgorithm algorithm)
        => algorithm switch {
            ChecksumAlgorithm.Crc32 => new ReflectedCrc32Calculator(ReflectedCrc32Calculator.Variant.Crc32),
            ChecksumAlgorithm.Crc32C => new ReflectedCrc32Calculator(ReflectedCrc32Calculator.Variant.Crc32C),
            ChecksumAlgorithm.Crc64 => new Crc64EcmaCalculator(),
            ChecksumAlgorithm.Adler32 => new Adler32Calculator(),
            var _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
        };
}