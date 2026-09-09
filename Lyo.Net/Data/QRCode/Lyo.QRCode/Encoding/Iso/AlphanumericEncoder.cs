namespace Lyo.QRCode.Encoding.Iso;

internal sealed partial class QRIsoEncoder
{
    /// <summary>
    /// Encodes alphanumeric characters (<c>0–9</c>, uppercase <c>A–Z</c>, space, <c>$</c>, <c>%</c>, <c>*</c>, <c>+</c>, <c>-</c>, period, <c>/</c>, colon) as QR binary.
    /// </summary>
    internal static class AlphanumericEncoder
    {
#if HAS_SPAN
        // C# 7.3+ inlines this byte array into the assembly read-only data section (faster, less heap).
        // See: https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-core-3-0/
        private static ReadOnlySpan<byte> Map
            => [
#else
        private static readonly byte[] Map = [
#endif
                // codes 0..31
                255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                255, 255,
                // codes 32..47  (space, ! " # $ % & ' ( ) * + , - . /)
                36, 255, 255, 255, 37, 38, 255, 255, 255, 255,
                39, 40, 255, 41, 42, 43,
                // codes 48..57  (0..9)
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                // codes 58..64  (: ; < = > ? @)
                44, 255, 255, 255, 255, 255, 255,
                // codes 65..90  (A..Z)
                10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
                20, 21, 22, 23, 24, 25, 26, 27, 28, 29,
                30, 31, 32, 33, 34, 35
                // no lookup above 90
            ];

        /// <summary>True if the character exists in the alphanumeric encoding table.</summary>
        public static bool CanEncode(char c) => c <= 90 && Map[c] != 255;

        /// <summary>Bits needed to encode alphanumeric text of the given length.</summary>
        /// <param name="textLength">Length of the alphanumeric text to encode.</param>
        /// <returns>Bit count required for the text.</returns>
        public static int GetBitLength(int textLength) => textLength / 2 * 11 + (textLength & 1) * 6;

        /// <summary>
        /// Encodes alphanumeric plain text as QR binary. Pairs pack into 11-bit groups; a leftover odd character uses 6 bits.
        /// </summary>
        /// <param name="plainText">Alphanumeric text to encode. Must contain only QR alphanumeric-mode characters.</param>
        /// <returns>BitArray of the encoded alphanumeric payload.</returns>
        public static BitArray GetBitArray(string plainText)
        {
            var codeText = new BitArray(GetBitLength(plainText.Length));
            WriteToBitArray(plainText, 0, plainText.Length, codeText, 0);
            return codeText;
        }

        /// <summary>
        /// Writes a slice of alphanumeric plain text into an existing BitArray. Pairs pack into 11-bit groups; a leftover odd character uses 6 bits.
        /// </summary>
        /// <param name="plainText">Alphanumeric text to encode. Must contain only QR alphanumeric-mode characters.</param>
        /// <param name="index">Starting index in the text to encode from.</param>
        /// <param name="count">Number of characters to encode.</param>
        /// <param name="codeText">Target BitArray to write into.</param>
        /// <param name="codeIndex">Starting index in the BitArray.</param>
        /// <returns>Next index in the BitArray after the last bit written.</returns>
        public static int WriteToBitArray(string plainText, int index, int count, BitArray codeText, int codeIndex)
        {
            // Encode character pairs.
            while (count >= 2) {
                // Map the pair through the alphanumeric table and combine.
                var dec = Map[plainText[index++]] * 45 + Map[plainText[index++]];
                // Store the combined value as 11 bits.
                codeIndex = DecToBin(dec, 11, codeText, codeIndex);
                count -= 2;
            }

            // Encode the leftover character when length is odd.
            if (count > 0)
                codeIndex = DecToBin(Map[plainText[index]], 6, codeText, codeIndex);

            return codeIndex;
        }
    }
}