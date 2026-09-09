#if HAS_SPAN
using System.Buffers;
#endif

namespace Lyo.QRCode.Encoding.Iso;

internal sealed partial class QRIsoEncoder
{
    /// <summary>Byte-mode data segment (UTF-8 and other text encodings).</summary>
    private sealed class ByteDataSegment : DataSegment
    {
        /// <summary>If true, always encode as UTF-8.</summary>
        public bool ForceUtf8 { get; }

        /// <summary>If true, include a UTF-8 BOM.</summary>
        public bool Utf8Bom { get; }

        /// <summary>ECI mode for this segment.</summary>
        public ECIMode ECIMode { get; }

        /// <summary>True when this segment writes an ECI mode indicator.</summary>
        public bool HasECIMode => ECIMode != ECIMode.Default;

        /// <summary>Encoding mode. Always Byte.</summary>
        public override EncodingMode EncodingMode => EncodingMode.Byte;

        /// <summary>Builds a ByteDataSegment.</summary>
        /// <param name="text">Text to encode</param>
        /// <param name="forceUtf8">If true, always encode as UTF-8</param>
        /// <param name="utf8Bom">If true, include a UTF-8 BOM</param>
        /// <param name="eciMode">ECI mode to use</param>
        public ByteDataSegment(string text, bool forceUtf8, bool utf8Bom, ECIMode eciMode)
            : base(text)
        {
            ForceUtf8 = forceUtf8;
            Utf8Bom = utf8Bom;
            ECIMode = eciMode;
        }

        /// <summary>Total bit length for this segment at a QR version.</summary>
        /// <param name="version">QR version (1-40, or -1 to -4 for Micro QR)</param>
        /// <returns>Bits required for this segment</returns>
        public override int GetBitLength(int version)
        {
            var modeIndicatorLength = HasECIMode ? 16 : 4;
            var countIndicatorLength = GetCountIndicatorLength(version, EncodingMode.Byte);
            var dataBitLength = GetPlainTextToBinaryByteBitLength(Text, ECIMode, Utf8Bom, ForceUtf8);
            var length = modeIndicatorLength + countIndicatorLength + dataBitLength;
            return length;
        }

        /// <summary>Writes this segment into an existing BitArray at the given index.</summary>
        /// <param name="bitArray">Target BitArray to write into</param>
        /// <param name="startIndex">Starting index in the BitArray</param>
        /// <param name="version">QR version (1-40, or -1 to -4 for Micro QR)</param>
        /// <returns>Next index in the BitArray after the last bit written</returns>
        public override int WriteTo(BitArray bitArray, int startIndex, int version)
        {
            var index = startIndex;

            // ECI mode, when present
            if (HasECIMode) {
                index = DecToBin((int)EncodingMode.ECI, 4, bitArray, index);
                index = DecToBin((int)ECIMode, 8, bitArray, index);
            }

            // mode indicator
            index = DecToBin((int)EncodingMode.Byte, 4, bitArray, index);

            // count indicator
            var dataBitLength = GetPlainTextToBinaryByteBitLength(Text, ECIMode, Utf8Bom, ForceUtf8);
            var characterCount = dataBitLength / 8;
            var countIndicatorLength = GetCountIndicatorLength(version, EncodingMode.Byte);
            index = DecToBin(characterCount, countIndicatorLength, bitArray, index);

            // payload bits
            index = PlainTextToBinaryByte(Text, ECIMode, Utf8Bom, ForceUtf8, bitArray, index);
            return index;
        }
    }

    private static readonly System.Text.Encoding ISO8859_1 =
#if NET5_0_OR_GREATER
        System.Text.Encoding.Latin1;
#else
        Encoding.GetEncoding(28591); // ISO-8859-1
#endif
    private static System.Text.Encoding? _iso8859_2;

    /// <summary>Picks the target encoding from the text and encoding flags.</summary>
    /// <param name="plainText">Text to encode.</param>
    /// <param name="eciMode">ECI mode that selects the character encoding.</param>
    /// <param name="utf8Bom">If true, include a UTF-8 BOM.</param>
    /// <param name="forceUtf8">If true, use UTF-8 even when the text fits ISO-8859-1.</param>
    /// <param name="includeUtf8Bom">Set to true when the UTF-8 BOM should be written.</param>
    /// <returns>Encoding to use for the text.</returns>
    private static System.Text.Encoding GetTargetEncoding(string plainText, ECIMode eciMode, bool utf8Bom, bool forceUtf8, out bool includeUtf8Bom)
    {
        System.Text.Encoding targetEncoding;

        // ISO-8859-1 when the text is valid Latin-1 and UTF-8 is not forced.
        if (eciMode == ECIMode.Default && !forceUtf8 && IsValidISO(plainText)) {
            targetEncoding = ISO8859_1;
            includeUtf8Bom = false;
        }
        else {
            // Encoding from the requested ECI mode.
            switch (eciMode) {
                case ECIMode.Iso8859_1:
                    // ISO-8859-1.
                    targetEncoding = ISO8859_1;
                    includeUtf8Bom = false;
                    break;
                case ECIMode.Iso8859_2:
                    // ISO-8859-2 is not built into .NET Core.
                    //
                    // Install System.Text.Encoding.CodePages and call Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
                    // before using this mode.
                    _iso8859_2 ??= System.Text.Encoding.GetEncoding(28592); // ISO-8859-2
                    // ISO-8859-2.
                    targetEncoding = _iso8859_2;
                    includeUtf8Bom = false;
                    break;
                case ECIMode.Default:
                case ECIMode.Utf8:
                default:
                    // UTF-8, with BOM when requested.
                    targetEncoding = System.Text.Encoding.UTF8;
                    includeUtf8Bom = utf8Bom;
                    break;
            }
        }

        return targetEncoding;
    }

    /// <summary>Bits needed to encode plain text in byte mode.</summary>
    /// <param name="plainText">Text to encode.</param>
    /// <param name="eciMode">ECI mode that selects the character encoding.</param>
    /// <param name="utf8Bom">If true, include a UTF-8 BOM.</param>
    /// <param name="forceUtf8">If true, use UTF-8 even when the text fits ISO-8859-1.</param>
    /// <returns>Bits required to encode the text.</returns>
    private static int GetPlainTextToBinaryByteBitLength(string plainText, ECIMode eciMode, bool utf8Bom, bool forceUtf8)
    {
        var targetEncoding = GetTargetEncoding(plainText, eciMode, utf8Bom, forceUtf8, out var includeUtf8Bom);
        var byteCount = targetEncoding.GetByteCount(plainText);
        return byteCount * 8 + (includeUtf8Bom ? 24 : 0);
    }

    /// <summary>Encodes plain text in byte mode. Character encodings are selected via ECI (Extended Channel Interpretations).</summary>
    /// <param name="plainText">Text to encode.</param>
    /// <param name="eciMode">ECI mode that selects the character encoding.</param>
    /// <param name="utf8Bom">If true, include a UTF-8 BOM.</param>
    /// <param name="forceUtf8">If true, use UTF-8 even when the text fits ISO-8859-1.</param>
    /// <returns>BitArray of the encoded text.</returns>
    /// <remarks>
    /// Payload is ISO-8859-1 unless a non-ISO-8859-1 character is present or UTF-8 is forced. That does not match the QR Code
    /// standard, which requires ECI when the encoding is not ISO-8859-1.
    /// </remarks>
    private static BitArray PlainTextToBinaryByte(string plainText, ECIMode eciMode, bool utf8Bom, bool forceUtf8)
    {
        var bitLength = GetPlainTextToBinaryByteBitLength(plainText, eciMode, utf8Bom, forceUtf8);
        var bitArray = new BitArray(bitLength);
        PlainTextToBinaryByte(plainText, eciMode, utf8Bom, forceUtf8, bitArray, 0);
        return bitArray;
    }

    /// <summary>Encodes plain text in byte mode into an existing BitArray at the given offset.</summary>
    /// <param name="plainText">Text to encode.</param>
    /// <param name="eciMode">ECI mode that selects the character encoding.</param>
    /// <param name="utf8Bom">If true, include a UTF-8 BOM.</param>
    /// <param name="forceUtf8">If true, use UTF-8 even when the text fits ISO-8859-1.</param>
    /// <param name="bitArray">Target BitArray. Must have room for the encoded data.</param>
    /// <param name="offset">Starting offset in the BitArray.</param>
    /// <returns>Next offset in the BitArray after the last bit written.</returns>
    /// <remarks>
    /// Payload is ISO-8859-1 unless a non-ISO-8859-1 character is present or UTF-8 is forced. That does not match the QR Code
    /// standard, which requires ECI when the encoding is not ISO-8859-1.
    /// </remarks>
    private static int PlainTextToBinaryByte(string plainText, ECIMode eciMode, bool utf8Bom, bool forceUtf8, BitArray bitArray, int offset)
    {
        var targetEncoding = GetTargetEncoding(plainText, eciMode, utf8Bom, forceUtf8, out var includeUtf8Bom);
#if HAS_SPAN
        // stackalloc for small buffers to skip the heap
        const int maxStackSizeInBytes = 512;
        var count = targetEncoding.GetByteCount(plainText);
        byte[]? bufferFromPool = null;
        var codeBytes = count <= maxStackSizeInBytes ? stackalloc byte[maxStackSizeInBytes] : bufferFromPool = ArrayPool<byte>.Shared.Rent(count);
        codeBytes = codeBytes[..count];
        targetEncoding.GetBytes(plainText, codeBytes);
#else
        byte[] codeBytes = targetEncoding.GetBytes(plainText);
#endif

        // Copy bytes into the BitArray
        if (includeUtf8Bom) {
            // UTF-8 preamble (EF BB BF)
            DecToBin(0xEF, 8, bitArray, offset);
            DecToBin(0xBB, 8, bitArray, offset + 8);
            DecToBin(0xBF, 8, bitArray, offset + 16);
            offset += 24;
        }

        CopyToBitArray(codeBytes, bitArray, offset);
        offset += (int)((uint)codeBytes.Length * 8);
#if HAS_SPAN
        if (bufferFromPool != null)
            ArrayPool<byte>.Shared.Return(bufferFromPool);
#endif
        return offset;
    }
}