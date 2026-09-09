namespace Lyo.QRCode.Encoding.Iso;

internal sealed partial class QRIsoEncoder
{
    /// <summary>Error-correction coding (ECC) info for one QR version and error-correction level.</summary>
    private struct ECCInfo
    {
        /// <summary>Builds an ECCInfo with the given properties.</summary>
        /// <param name="version">QR version number.</param>
        /// <param name="errorCorrectionLevel">Error-correction level used in the QR code.</param>
        /// <param name="totalDataCodewords">Data codewords for this version and error-correction level.</param>
        /// <param name="eccPerBlock">Error-correction codewords per block.</param>
        /// <param name="blocksInGroup1">Blocks in group 1.</param>
        /// <param name="codewordsInGroup1">Codewords in each group-1 block.</param>
        /// <param name="blocksInGroup2">Blocks in group 2, if any.</param>
        /// <param name="codewordsInGroup2">Codewords in each group-2 block, if any.</param>
        public ECCInfo(
            int version,
            ECCLevel errorCorrectionLevel,
            int totalDataCodewords,
            int eccPerBlock,
            int blocksInGroup1,
            int codewordsInGroup1,
            int blocksInGroup2,
            int codewordsInGroup2)
        {
            Version = version;
            ErrorCorrectionLevel = errorCorrectionLevel;
            TotalDataCodewords = totalDataCodewords;
            TotalDataBits = totalDataCodewords * 8;
            ECCPerBlock = eccPerBlock;
            BlocksInGroup1 = blocksInGroup1;
            CodewordsInGroup1 = codewordsInGroup1;
            BlocksInGroup2 = blocksInGroup2;
            CodewordsInGroup2 = codewordsInGroup2;
        }

        /// <summary>Builds an ECCInfo for Micro QR codes.</summary>
        /// <param name="version">QR version number.</param>
        /// <param name="errorCorrectionLevel">Error-correction level used in the QR code.</param>
        /// <param name="totalDataCodewords">Data codewords for this version and error-correction level.</param>
        /// <param name="totalDataBits">Data bits for this version and error-correction level.</param>
        /// <param name="eccPerBlock">Error-correction codewords per block.</param>
        public ECCInfo(int version, ECCLevel errorCorrectionLevel, int totalDataCodewords, int totalDataBits, int eccPerBlock)
        {
            Version = version;
            ErrorCorrectionLevel = errorCorrectionLevel;
            TotalDataCodewords = totalDataCodewords;
            TotalDataBits = totalDataBits;
            ECCPerBlock = eccPerBlock;
            BlocksInGroup1 = 1;
            CodewordsInGroup1 = totalDataCodewords;
            BlocksInGroup2 = 0;
            CodewordsInGroup2 = 0;
        }

        /// <summary>QR version number.</summary>
        public int Version { get; }

        /// <summary>Error-correction level of the QR code.</summary>
        public ECCLevel ErrorCorrectionLevel { get; }

        /// <summary>Data codewords for this version and error-correction level.</summary>
        public int TotalDataCodewords { get; }

        /// <summary>Data codewords for this version and error-correction level.</summary>
        public int TotalDataBits { get; }

        /// <summary>Error-correction codewords per block.</summary>
        public int ECCPerBlock { get; }

        /// <summary>Blocks in group 1.</summary>
        public int BlocksInGroup1 { get; }

        /// <summary>Codewords in each group-1 block.</summary>
        public int CodewordsInGroup1 { get; }

        /// <summary>Blocks in group 2, if any.</summary>
        public int BlocksInGroup2 { get; }

        /// <summary>Codewords in each group-2 block, if any.</summary>
        public int CodewordsInGroup2 { get; }
    }
}