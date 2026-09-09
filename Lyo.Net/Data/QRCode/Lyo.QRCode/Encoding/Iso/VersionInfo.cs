namespace Lyo.QRCode.Encoding.Iso;

internal sealed partial class QRIsoEncoder
{
    /// <summary>Version-specific information for a QR code.</summary>
    private struct VersionInfo
    {
        /// <summary>Builds a <see cref="VersionInfo" /> with a version number and its details.</summary>
        /// <param name="version">QR version number. Each version has a different module layout.</param>
        /// <param name="versionInfoDetails">Details for error-correction levels and capacity per encoding mode.</param>
        public VersionInfo(int version, List<VersionInfoDetails> versionInfoDetails)
        {
            Version = version;
            Details = versionInfoDetails;
        }

        /// <summary>QR version number. Each number specifies a different QR matrix size.</summary>
        public int Version { get; }

        /// <summary>Details for this version, including error-correction levels and encoding capacities.</summary>
        public List<VersionInfoDetails> Details { get; }
    }
}