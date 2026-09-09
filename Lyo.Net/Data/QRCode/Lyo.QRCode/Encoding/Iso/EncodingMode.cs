namespace Lyo.QRCode.Encoding.Iso;

internal sealed partial class QRIsoEncoder
{
    /// <summary>Encoding modes for characters in a QR code.</summary>
    internal enum EncodingMode
    {
        /// <summary>Numeric mode for digits 0-9. Three characters encode into 10 bits.</summary>
        Numeric = 1,

        /// <summary>Alphanumeric mode for 0-9, A-Z, space, and some punctuation. Two characters encode into 11 bits.</summary>
        Alphanumeric = 2,

        /// <summary>
        /// Byte mode, mainly ISO-8859-1. Each character encodes into 8 bits. Combined with ECI it can use other character sets.
        /// </summary>
        Byte = 4,

        /// <summary>
        /// Kanji mode for Shift JIS, mainly Japanese Kanji and Kana. One character encodes into 13 bits. QRCoder does not support this mode today.
        /// </summary>
        Kanji = 8,

        /// <summary>
        /// Extended Channel Interpretation (ECI) mode. Names a character set with an 8-bit number, then uses one of the other encoding modes so byte encoding can follow other
        /// global text encodings.
        /// </summary>
        ECI = 7
    }
}