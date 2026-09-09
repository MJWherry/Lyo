namespace Lyo.QRCode.Encoding.Iso;

internal sealed partial class QRIsoEncoder
{
    /// <summary>Per-level capacities in each encoding mode for one QR version.</summary>
    private struct VersionInfoDetails
    {
        /// <summary>Builds a <see cref="VersionInfoDetails" /> with an error-correction level and capacity per encoding mode.</summary>
        /// <param name="errorCorrectionLevel">How much of the symbol can be restored if it is damaged.</param>
        /// <param name="capacityDict">Capacity for each encoding mode at this error-correction level.</param>
        public VersionInfoDetails(ECCLevel errorCorrectionLevel, Dictionary<EncodingMode, int> capacityDict)
        {
            ErrorCorrectionLevel = errorCorrectionLevel;
            CapacityDict = capacityDict;
        }

        /// <summary>Error-correction level. Controls how robust the symbol is against damage.</summary>
        public ECCLevel ErrorCorrectionLevel { get; }

        /// <summary>
        /// Capacities of each encoding mode at this error-correction level. Each value is how many characters that mode can encode.
        /// </summary>
        public Dictionary<EncodingMode, int> CapacityDict { get; }
    }
}