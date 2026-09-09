namespace Lyo.QRCode.Encoding.Iso;

internal sealed partial class QRIsoEncoder
{
    /// <summary>
    /// Alignment pattern used in QR codes so the symbol stays readable when it is somewhat distorted. Each QR version has its own alignment-pattern locations; this
    /// struct holds those points.
    /// </summary>
    private struct AlignmentPattern
    {
        /// <summary>QR version. Higher versions use more, more complex alignment patterns.</summary>
        public int Version;

        /// <summary>Centers of alignment patterns in the QR matrix.</summary>
        public List<Point> PatternPositions;
    }
}