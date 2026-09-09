namespace Lyo.Hashing.Internal;

/// <summary>
/// Incremental, allocation-free state for one <see cref="ChecksumAlgorithm" />. <see cref="Append" /> folds bytes; <see cref="GetCurrentValue" />
/// does not mutate, so it can be read while more data is still appended. Implementations are the cross-target source of truth (used on netstandard2.0, for streaming on
/// every target, and for algorithms <c>System.IO.Hashing</c> does not provide).
/// </summary>
internal abstract class ChecksumCalculator
{
    /// <summary>Byte count of the big-endian output (4 for 32-bit checksums, 8 for CRC-64).</summary>
    public abstract int HashSizeInBytes { get; }

    /// <summary>Folds <paramref name="data" /> into the running checksum.</summary>
    public abstract void Append(ReadOnlySpan<byte> data);

    /// <summary>Checksum of everything appended so far, without changing state. 32-bit results sit in the low bits.</summary>
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