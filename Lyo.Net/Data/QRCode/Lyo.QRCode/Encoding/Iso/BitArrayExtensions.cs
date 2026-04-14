namespace Lyo.QRCode.Encoding.Iso;

/// <summary>Helper methods for <see cref="BitArray" /> (from MIT-licensed QRCoder).</summary>
internal static class BitArrayExtensions
{
    /// <summary>Copies <paramref name="count" /> bits from <paramref name="source" /> into <paramref name="destination" />.</summary>
    public static int CopyTo(this BitArray source, BitArray destination, int sourceOffset, int destinationOffset, int count)
    {
        for (var i = 0; i < count; i++)
            destination[destinationOffset + i] = source[sourceOffset + i];

        return destinationOffset + count;
    }
}