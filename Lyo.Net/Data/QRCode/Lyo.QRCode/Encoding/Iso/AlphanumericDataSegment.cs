namespace Lyo.QRCode.Encoding.Iso;

internal sealed partial class QRIsoEncoder
{
    /// <summary>Data segment for alphanumeric encoding.</summary>
    private sealed class AlphanumericDataSegment : DataSegment
    {
        /// <summary>Encoding mode. Always Alphanumeric.</summary>
        public override EncodingMode EncodingMode => EncodingMode.Alphanumeric;

        /// <summary>Builds an AlphanumericDataSegment.</summary>
        /// <param name="alphanumericText">Alphanumeric text to encode</param>
        public AlphanumericDataSegment(string alphanumericText)
            : base(alphanumericText) { }

        /// <summary>Total bit length for this segment at a QR version.</summary>
        /// <param name="version">QR version (1-40, or -1 to -4 for Micro QR)</param>
        /// <returns>Bits required for this segment</returns>
        public override int GetBitLength(int version) => GetBitLength(Text.Length, version);

        /// <summary>Total bit length for alphanumeric text of a given length at a QR version. Includes mode indicator, count indicator, and data bits.</summary>
        /// <param name="textLength">Length of the alphanumeric text</param>
        /// <param name="version">QR version (1-40, or -1 to -4 for Micro QR)</param>
        /// <returns>Bits required</returns>
        public static int GetBitLength(int textLength, int version)
        {
            var modeIndicatorLength = 4;
            var countIndicatorLength = GetCountIndicatorLength(version, EncodingMode.Alphanumeric);
            var dataLength = AlphanumericEncoder.GetBitLength(textLength);
            var length = modeIndicatorLength + countIndicatorLength + dataLength;
            return length;
        }

        /// <summary>Writes this segment into an existing BitArray at the given index.</summary>
        /// <param name="bitArray">Target BitArray to write into</param>
        /// <param name="startIndex">Starting index in the BitArray</param>
        /// <param name="version">QR version (1-40, or -1 to -4 for Micro QR)</param>
        /// <returns>Next index in the BitArray after the last bit written</returns>
        public override int WriteTo(BitArray bitArray, int startIndex, int version) => WriteTo(Text, 0, Text.Length, bitArray, startIndex, version);

        /// <summary>Writes a slice of alphanumeric text into a BitArray at the given index. Includes mode indicator, count indicator, and data bits.</summary>
        /// <param name="text">Full alphanumeric text</param>
        /// <param name="offset">Starting index in the text to encode from</param>
        /// <param name="length">Number of characters to encode</param>
        /// <param name="bitArray">Target BitArray to write into</param>
        /// <param name="bitIndex">Starting index in the BitArray</param>
        /// <param name="version">QR version (1-40, or -1 to -4 for Micro QR)</param>
        /// <returns>Next index in the BitArray after the last bit written</returns>
        public static int WriteTo(string text, int offset, int length, BitArray bitArray, int bitIndex, int version)
        {
            // mode indicator
            bitIndex = DecToBin((int)EncodingMode.Alphanumeric, 4, bitArray, bitIndex);

            // count indicator
            var countIndicatorLength = GetCountIndicatorLength(version, EncodingMode.Alphanumeric);
            bitIndex = DecToBin(length, countIndicatorLength, bitArray, bitIndex);

            // encode alphanumeric text
            bitIndex = AlphanumericEncoder.WriteToBitArray(text, offset, length, bitArray, bitIndex);
            return bitIndex;
        }
    }
}