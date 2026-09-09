// ReSharper disable InconsistentNaming

#pragma warning disable IDE0018 // Inline variable declaration -- false positive

namespace Lyo.QRCode.Encoding.Iso;

internal sealed partial class QRIsoEncoder
{
    /// <summary>
    /// Segment that picks Numeric, Alphanumeric, or Byte mode from character runs to shrink total bit length.
    /// Follows ISO/IEC 18004:2015 Annex J.2. Kanji mode is not supported.
    /// </summary>
    private sealed class OptimizedLatin1DataSegment : DataSegment
    {
        /// <summary>Encoding mode. Used only for DataTooLongException.</summary>
        public override EncodingMode EncodingMode => EncodingMode.Byte;

        /// <summary>Builds an OptimizedLatin1DataSegment.</summary>
        /// <param name="plainText">Text to encode with mode switching</param>
        public OptimizedLatin1DataSegment(string plainText)
            : base(plainText) { }

        /// <summary>True if every character is in the ISO-8859-1 range (0x00-0xFF) and can use optimized Latin-1 encoding.</summary>
        /// <param name="plainText">Text to check</param>
        /// <returns>True if the text can be encoded as ISO-8859-1</returns>
        public static bool CanEncode(string plainText) => IsValidISO(plainText);

        /// <summary>Total bit length for this segment at a QR version.</summary>
        /// <param name="version">QR version (1-40, or -1 to -4 for Micro QR)</param>
        /// <returns>Bits required for this segment</returns>
        public override int GetBitLength(int version)
        {
            if (string.IsNullOrEmpty(Text))
                return 0;

            var totalBits = 0;
            var mode = SelectInitialMode(Text, 0, version);
            var startPos = 0;
            do {
                // How far the current mode runs
                EncodingMode nextMode;
                var segmentEnd = mode switch {
                    EncodingMode.Byte => ProcessByteMode(Text, startPos, version, out nextMode),
                    EncodingMode.Alphanumeric => ProcessAlphanumericMode(Text, startPos, version, out nextMode),
                    EncodingMode.Numeric => ProcessNumericMode(Text, startPos, version, out nextMode),
                    var _ => throw new InvalidOperationException("Unsupported encoding mode")
                };

                var segmentLength = segmentEnd - startPos;
                totalBits += mode switch {
                    EncodingMode.Numeric => NumericDataSegment.GetBitLength(segmentLength, version),
                    EncodingMode.Alphanumeric => AlphanumericDataSegment.GetBitLength(segmentLength, version),
                    EncodingMode.Byte => GetByteBitLength(segmentLength, version),
                    var _ => throw new InvalidOperationException("Unsupported encoding mode")
                };

                // Advance to the next run
                startPos = segmentEnd;
                mode = nextMode;
            } while (startPos < Text.Length);

            return totalBits;
        }

        /// <summary>Bit length of a byte-mode segment.</summary>
        private static int GetByteBitLength(int textLength, int version)
        {
            var modeIndicatorLength = 4;
            var countIndicatorLength = GetCountIndicatorLength(version, EncodingMode.Byte);
            var dataLength = textLength * 8; // ISO-8859-1: 8 bits per char
            return modeIndicatorLength + countIndicatorLength + dataLength;
        }

        /// <summary>Writes this segment into an existing BitArray at the given index.</summary>
        /// <param name="bitArray">Target BitArray to write into</param>
        /// <param name="startIndex">Starting index in the BitArray</param>
        /// <param name="version">QR version (1-40, or -1 to -4 for Micro QR)</param>
        /// <returns>Next index in the BitArray after the last bit written</returns>
        public override int WriteTo(BitArray bitArray, int startIndex, int version)
        {
            if (string.IsNullOrEmpty(Text))
                return startIndex;

            var bitIndex = startIndex;
            var mode = SelectInitialMode(Text, 0, version);
            var startPos = 0;
            do {
                // How far the current mode runs
                EncodingMode nextMode;
                var segmentEnd = mode switch {
                    EncodingMode.Byte => ProcessByteMode(Text, startPos, version, out nextMode),
                    EncodingMode.Alphanumeric => ProcessAlphanumericMode(Text, startPos, version, out nextMode),
                    EncodingMode.Numeric => ProcessNumericMode(Text, startPos, version, out nextMode),
                    var _ => throw new InvalidOperationException("Unsupported encoding mode")
                };

                var segmentLength = segmentEnd - startPos;
                bitIndex = mode switch {
                    EncodingMode.Numeric => NumericDataSegment.WriteTo(Text, startPos, segmentLength, bitArray, bitIndex, version),
                    EncodingMode.Alphanumeric => AlphanumericDataSegment.WriteTo(Text, startPos, segmentLength, bitArray, bitIndex, version),
                    EncodingMode.Byte => WriteByteSegment(Text, startPos, segmentLength, bitArray, bitIndex, version),
                    var _ => throw new InvalidOperationException("Unsupported encoding mode")
                };

                // Advance to the next run
                startPos = segmentEnd;
                mode = nextMode;
            } while (startPos < Text.Length);

            return bitIndex;
        }

        /// <summary>Writes a byte-mode segment into the BitArray.</summary>
        private static int WriteByteSegment(string text, int offset, int length, BitArray bitArray, int bitIndex, int version)
        {
            // mode indicator
            bitIndex = DecToBin((int)EncodingMode.Byte, 4, bitArray, bitIndex);

            // count indicator
            var countIndicatorLength = GetCountIndicatorLength(version, EncodingMode.Byte);
            bitIndex = DecToBin(length, countIndicatorLength, bitArray, bitIndex);

            // ISO-8859-1 payload
            for (var i = 0; i < length; i++)
                bitIndex = DecToBin(text[offset + i], 8, bitArray, bitIndex);

            return bitIndex;
        }

        // Picks the starting mode from the first character(s).
        // ISO/IEC 18004:2015 Annex J.2 section a.
        private static EncodingMode SelectInitialMode(string text, int startPos, int version)
        {
            var c = text[startPos];

            // Rule a.1: first data is Byte-only → Byte mode
            if (!IsAlphanumeric(c))
                return EncodingMode.Byte;

            // Rule a.4: numeric start, fewer than [4,4,5] digits then Byte-only data → Byte mode
            if (IsNumeric(c)) {
                var numericCount = CountConsecutive(text, startPos, IsNumeric);
                var threshold = GetBreakpoint(version, 4, 4, 5);
                if (numericCount < threshold) {
                    var nextPos = startPos + numericCount;
                    if (nextPos < text.Length && !IsAlphanumeric(text[nextPos]))
                        return EncodingMode.Byte;
                }

                // ELSE IF fewer than [7-9] digits then Alphanumeric-only data → Alphanumeric; otherwise Numeric
                threshold = GetBreakpoint(version, 7, 8, 9);
                if (numericCount < threshold) {
                    var nextPos = startPos + numericCount;
                    if (nextPos < text.Length && IsAlphanumeric(text[nextPos]))
                        return EncodingMode.Alphanumeric;
                }

                return EncodingMode.Numeric;
            }

            // Rule a.3: Alphanumeric-only start, fewer than [6-8] chars then remaining Byte set → Byte mode
            var alphanumericCount = CountConsecutive(text, startPos, IsAlphanumeric);
            var alphaThreshold = GetBreakpoint(version, 6, 7, 8);
            if (alphanumericCount < alphaThreshold) {
                var nextPos = startPos + alphanumericCount;
                if (nextPos < text.Length && !IsAlphanumeric(text[nextPos]))
                    return EncodingMode.Byte;
            }

            return EncodingMode.Alphanumeric;
        }

        // Walks Byte mode and decides when to switch.
        // ISO/IEC 18004:2015 Annex J.2 section b.
        private static int ProcessByteMode(string text, int startPos, int version, out EncodingMode nextMode)
        {
            var pos = startPos;
            var numericThreshold = GetBreakpoint(version, 6, 8, 9);
            var alphaThreshold = GetBreakpoint(version, 11, 15, 16);
            while (pos < text.Length) {
                // Rule b.3: at least [6,8,9] Numeric chars before more Byte-only data → Numeric mode
                var numericCount = CountConsecutive(text, pos, IsNumeric);
                if (numericCount >= numericThreshold) {
                    nextMode = EncodingMode.Numeric;
                    return pos;
                }

                // Rule b.2: at least [11,15,16] Alphanumeric-only chars before more Byte-only data → Alphanumeric mode
                var alphanumericCount = CountConsecutive(text, pos, IsAlphanumeric);
                if (alphanumericCount >= alphaThreshold) {
                    nextMode = EncodingMode.Alphanumeric;
                    return pos;
                }

                // Stay in Byte mode
                pos++;
            }

            nextMode = EncodingMode.Byte;
            return pos;
        }

        // Walks Alphanumeric mode and decides when to switch.
        // ISO/IEC 18004:2015 Annex J.2 section c.
        private static int ProcessAlphanumericMode(string text, int startPos, int version, out EncodingMode nextMode)
        {
            var pos = startPos;
            var threshold = GetBreakpoint(version, 13, 15, 17);
            while (pos < text.Length) {
                var c = text[pos];

                // Rule c.2: any Byte-only character → Byte mode
                if (!IsAlphanumeric(c)) {
                    nextMode = EncodingMode.Byte;
                    return pos;
                }

                // Rule c.3: at least [13,15,17] Numeric chars before more Alphanumeric-only data → Numeric mode
                var numericCount = CountConsecutive(text, pos, IsNumeric);
                if (numericCount >= threshold) {
                    nextMode = EncodingMode.Numeric;
                    return pos;
                }

                // Stay in Alphanumeric mode
                pos++;
            }

            nextMode = EncodingMode.Alphanumeric;
            return pos;
        }

        // Walks Numeric mode and decides when to switch.
        // ISO/IEC 18004:2015 Annex J.2 section d.
        private static int ProcessNumericMode(string text, int startPos, int version, out EncodingMode nextMode)
        {
            var pos = startPos;
            while (pos < text.Length) {
                var c = text[pos];

                // Rule d.2: any Byte-only character → Byte mode
                // Rule d.3: any Alphanumeric-only character → Alphanumeric mode

                // Delegates to the initial-mode picker instead of the two rules above:
                if (!IsNumeric(c)) {
                    nextMode = SelectInitialMode(text, pos, version);
                    return pos;
                }

                // Stay in Numeric mode
                pos++;
            }

            nextMode = EncodingMode.Numeric;
            return pos;
        }

        // Breakpoint for the QR version band.
        // ISO/IEC 18004:2015 Annex J.2 uses different thresholds by version:
        // - Versions 1-9: v1_9
        // - Versions 10-26: v10_26
        // - Versions 27-40: v27_40
        private static int GetBreakpoint(int version, int v1_9, int v10_26, int v27_40)
        {
            if (version < 10)
                return v1_9;

            if (version < 27)
                return v10_26;

            return v27_40;
        }

        // Consecutive characters from startPos that match the predicate.
        private static int CountConsecutive(string text, int startPos, Func<char, bool> predicate)
        {
            var count = 0;
            for (var i = startPos; i < text.Length && predicate(text[i]); i++)
                count++;

            return count;
        }

        // True if the character is a digit (0-9).
        private static bool IsNumeric(char c) => IsInRange(c, '0', '9');

        // True if the character can be encoded in alphanumeric mode.
        private static bool IsAlphanumeric(char c) => AlphanumericEncoder.CanEncode(c);
    }
}