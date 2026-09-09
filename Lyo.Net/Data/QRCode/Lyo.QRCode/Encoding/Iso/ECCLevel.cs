namespace Lyo.QRCode.Encoding.Iso;

internal sealed partial class QRIsoEncoder
{
    /// <summary>
    /// Error-correction levels for QR codes. Each level is the fraction of data that can be recovered if the symbol is partly obscured or damaged.
    /// </summary>
    public enum ECCLevel
    {
        /// <summary>
        /// Default error-correction level. Selects Level M (Medium) unless the payload says otherwise. Level M recovers about 15% of the data, a balance of capacity and
        /// recovery.
        /// </summary>
        Default = -1,

        /// <summary>Level L: low error correction (about 7% recoverable). Highest data density.</summary>
        L = 0,

        /// <summary>Level M: medium error correction (about 15% recoverable). Balance of capacity and recovery.</summary>
        M = 1,

        /// <summary>Level Q: quartile error correction (about 25% recoverable). Stronger recovery, less capacity.</summary>
        Q = 2,

        /// <summary>
        /// Level H: high error correction (about 30% recoverable). Strongest recovery, suited to places with a high risk of data loss.
        /// </summary>
        H = 3
    }
}