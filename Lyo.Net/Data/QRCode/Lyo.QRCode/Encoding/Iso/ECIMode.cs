#pragma warning disable CA1707 // Underscore in identifier

namespace Lyo.QRCode.Encoding.Iso;

internal sealed partial class QRIsoEncoder
{
    /// <summary>
    /// Extended Channel Interpretation (ECI) modes used in QR codes for character encodings other than the default ISO-8859-1.
    /// </summary>
    public enum ECIMode
    {
        /// <summary>
        /// Default encoding (usually ISO-8859-1). Used when no ECI mode is named. Assumed in basic QR codes that do not need extended character sets.
        /// </summary>
        Default = 0,

        /// <summary>
        /// ISO-8859-1, covering most Western European languages (English, French, German, Spanish, and similar).
        /// </summary>
        Iso8859_1 = 3,

        /// <summary>
        /// ISO-8859-2, mainly Central and Eastern European languages (Polish, Czech, Slovak, Hungarian, Romanian, and similar).
        /// </summary>
        Iso8859_2 = 4,

        /// <summary>UTF-8 encoding. Can encode any Unicode character. Useful for multi-language QR content.</summary>
        Utf8 = 26
    }
}