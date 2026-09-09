namespace Lyo.QRCode.Encoding.Iso;

internal sealed partial class QRIsoEncoder
{
    /// <summary>
    /// Encodes numeric plain text as QR binary. Groups of up to 3 digits use 10 bits; leftover digits use fewer bits.
    /// </summary>
    /// <param name="plainText">Numeric text to encode. Must contain only digits.</param>
    /// <returns>BitArray of the encoded numeric payload.</returns>
    private static BitArray PlainTextToBinaryNumeric(string plainText)
    {
        // BitArray size for the text.
        // Three digits → 10 bits; leftover two digits → 7 bits; leftover one digit → 4 bits.
        var bitArray = new BitArray(plainText.Length / 3 * 10 + (plainText.Length % 3 == 1 ? 4 : plainText.Length % 3 == 2 ? 7 : 0));
        PlainTextToBinaryNumeric(plainText, 0, plainText.Length, bitArray, 0);
        return bitArray;
    }

    /// <summary>
    /// Encodes a slice of numeric plain text into an existing BitArray. Groups of up to 3 digits use 10 bits; leftover digits use fewer bits.
    /// </summary>
    /// <param name="plainText">Numeric text to encode. Must contain only digits.</param>
    /// <param name="offset">Starting index in the text to encode from.</param>
    /// <param name="length">Number of characters to encode.</param>
    /// <param name="bitArray">Target BitArray to write into.</param>
    /// <param name="bitIndex">Starting index in the BitArray.</param>
    /// <returns>Next index in the BitArray after the last bit written.</returns>
    private static int PlainTextToBinaryNumeric(string plainText, int offset, int length, BitArray bitArray, int bitIndex)
    {
        var endIndex = offset + length;

        // Encode each three-digit group.
        for (var i = offset; i < endIndex - 2; i += 3) {
            // Next three characters as a decimal integer.
#if HAS_SPAN
            var dec = int.Parse(plainText.AsSpan(i, 3), NumberStyles.None, CultureInfo.InvariantCulture);
#else
            var dec = int.Parse(plainText.Substring(i, 3), NumberStyles.None, CultureInfo.InvariantCulture);
#endif
            // Store the decimal as 10 bits.
            bitIndex = DecToBin(dec, 10, bitArray, bitIndex);
            offset += 3;
            length -= 3;
        }

        // Leftover digits when the length is not a multiple of three.
        if (length > 0) // two leftover digits → 7 bits; one leftover digit → 4 bits
        {
#if HAS_SPAN
            var dec = int.Parse(plainText.AsSpan(offset, length), NumberStyles.None, CultureInfo.InvariantCulture);
#else
            var dec = int.Parse(plainText.Substring(offset, length), NumberStyles.None, CultureInfo.InvariantCulture);
#endif
            bitIndex = DecToBin(dec, length == 2 ? 7 : 4, bitArray, bitIndex);
        }

        return bitIndex;
    }

    /// <summary>Data segment for numeric encoding.</summary>
    private sealed class NumericDataSegment : DataSegment
    {
        /// <summary>Encoding mode. Always Numeric.</summary>
        public override EncodingMode EncodingMode => EncodingMode.Numeric;

        /// <summary>Builds a NumericDataSegment.</summary>
        /// <param name="numericText">Numeric text to encode (digits only)</param>
        public NumericDataSegment(string numericText)
            : base(numericText) { }

        /// <summary>Total bit length for this segment at a QR version.</summary>
        /// <param name="version">QR version (1-40, or -1 to -4 for Micro QR)</param>
        /// <returns>Bits required for this segment</returns>
        public override int GetBitLength(int version) => GetBitLength(Text.Length, version);

        /// <summary>Total bit length for numeric text of a given length at a QR version. Includes mode indicator, count indicator, and data bits.</summary>
        /// <param name="textLength">Length of the numeric text</param>
        /// <param name="version">QR version (1-40, or -1 to -4 for Micro QR)</param>
        /// <returns>Bits required</returns>
        public static int GetBitLength(int textLength, int version)
        {
            var modeIndicatorLength = 4;
            var countIndicatorLength = GetCountIndicatorLength(version, EncodingMode.Numeric);
            var dataLength = textLength / 3 * 10 + (textLength % 3 == 1 ? 4 : textLength % 3 == 2 ? 7 : 0);
            var length = modeIndicatorLength + countIndicatorLength + dataLength;
            return length;
        }

        /// <summary>Writes this segment into an existing BitArray at the given index.</summary>
        /// <param name="bitArray">Target BitArray to write into</param>
        /// <param name="startIndex">Starting index in the BitArray</param>
        /// <param name="version">QR version (1-40, or -1 to -4 for Micro QR)</param>
        /// <returns>Next index in the BitArray after the last bit written</returns>
        public override int WriteTo(BitArray bitArray, int startIndex, int version) => WriteTo(Text, 0, Text.Length, bitArray, startIndex, version);

        /// <summary>Writes a slice of numeric text into a BitArray at the given index. Includes mode indicator, count indicator, and data bits.</summary>
        /// <param name="text">Full numeric text</param>
        /// <param name="startIndex">Starting index in the text to encode from</param>
        /// <param name="length">Number of characters to encode</param>
        /// <param name="bitArray">Target BitArray to write into</param>
        /// <param name="bitIndex">Starting index in the BitArray</param>
        /// <param name="version">QR version (1-40, or -1 to -4 for Micro QR)</param>
        /// <returns>Next index in the BitArray after the last bit written</returns>
        public static int WriteTo(string text, int startIndex, int length, BitArray bitArray, int bitIndex, int version)
        {
            var index = bitIndex;

            // mode indicator
            index = DecToBin((int)EncodingMode.Numeric, 4, bitArray, index);

            // count indicator
            var countIndicatorLength = GetCountIndicatorLength(version, EncodingMode.Numeric);
            index = DecToBin(length, countIndicatorLength, bitArray, index);

            // encode numeric text
            index = PlainTextToBinaryNumeric(text, startIndex, length, bitArray, index);
            return index;
        }
    }
}