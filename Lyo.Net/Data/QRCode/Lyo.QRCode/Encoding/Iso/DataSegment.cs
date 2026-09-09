namespace Lyo.QRCode.Encoding.Iso;

internal sealed partial class QRIsoEncoder
{
    /// <summary>Abstract data segment for QR encoding.</summary>
    private abstract class DataSegment
    {
        /// <summary>Text to encode.</summary>
        public string Text { get; }

        /// <summary>Encoding mode for this segment (Numeric, Alphanumeric, Byte, and similar).</summary>
        public abstract EncodingMode EncodingMode { get; }

        /// <summary>Builds a DataSegment.</summary>
        /// <param name="text">Text to encode</param>
        protected DataSegment(string text) => Text = text;

        /// <summary>Writes this segment into an existing BitArray at the given index for a QR version. Chains to the next segment when one is present.</summary>
        /// <param name="bitArray">Target BitArray to write into</param>
        /// <param name="startIndex">Index in the BitArray where writing starts</param>
        /// <param name="version">QR version (1-40, or -1 to -4 for Micro QR)</param>
        /// <returns>Next index in the BitArray after the last bit written</returns>
        public abstract int WriteTo(BitArray bitArray, int startIndex, int version);

        /// <summary>Builds a complete BitArray from this segment for a QR version.</summary>
        /// <param name="version">QR version (1-40, or -1 to -4 for Micro QR)</param>
        /// <returns>BitArray that holds the encoded segment</returns>
        public BitArray ToBitArray(int version)
        {
            var bitArray = new BitArray(GetBitLength(version));
            WriteTo(bitArray, 0, version);
            return bitArray;
        }

        /// <summary>Total bit length for this segment at a QR version, including every chained segment.</summary>
        /// <param name="version">QR version (1-40, or -1 to -4 for Micro QR)</param>
        /// <returns>Bits needed for this segment, including mode indicator, count indicator, and data</returns>
        public abstract int GetBitLength(int version);
    }
}