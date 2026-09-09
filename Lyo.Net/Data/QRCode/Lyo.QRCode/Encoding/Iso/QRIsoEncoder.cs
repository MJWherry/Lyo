using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Lyo.QRCode.Encoding.Iso;

/// <summary>ISO/IEC 18004 QR symbol builder (internal). Encoding logic from the MIT-licensed QRCoder project.</summary>
internal sealed partial class QRIsoEncoder
{
    private static readonly BitArray RepeatingPattern = new(
    [
        true, true, true, false, true, true, false, false, false, false,
        false, true, false, false, false, true
    ]);

    private static readonly BitArray GetFormatGenerator = new(
    [
        true, false, true, false, false, true, true, false, true, true,
        true
    ]);

    private static readonly BitArray GetFormatMask = new(
    [
        true, false, true, false, true, false, false, false, false, false,
        true, false, false, true, false
    ]);

    private static readonly BitArray GetFormatMicroMask = new(
    [
        true, false, false, false, true, false, false, false, true, false,
        false, false, true, false, true
    ]);

    private static readonly BitArray GetVersionGenerator = new(
    [
        true, true, true, true, true, false, false, true, false, false,
        true, false, true
    ]);

    private static readonly BitArray EmptyBitArray = new(0);

    /// <summary>Builds QR data that a renderer can turn into a graphic.</summary>
    /// <param name="plainText">Payload to encode in the QR code</param>
    /// <param name="eccLevel">Error-correction level</param>
    /// <param name="forceUtf8">If true, encode in UTF-8 mode</param>
    /// <param name="utf8Bom">If true, include a UTF-8 BOM</param>
    /// <param name="eciMode">ECI mode to use</param>
    /// <param name="requestedVersion">Fixed target QR version, or -1 to choose</param>
    /// <exception cref="DataTooLongException">Raised when the payload is too large for a QR code.</exception>
    /// <returns>Raw QR data for rendering.</returns>
    public static QRIsoMatrix GenerateQrCode(
        string plainText,
        ECCLevel eccLevel,
        bool forceUtf8 = false,
        bool utf8Bom = false,
        ECIMode eciMode = ECIMode.Default,
        int requestedVersion = -1)
    {
        eccLevel = ValidateECCLevel(eccLevel);
        // Segment from plain text
        var segment = CreateDataSegment(plainText, forceUtf8, utf8Bom, eciMode);
        // Version from segment bit length
        var version = DetermineVersion(segment, eccLevel, requestedVersion);
        // Full bit array for that version
        var completeBitArray = segment.ToBitArray(version);
        return GenerateQrCode(completeBitArray, eccLevel, version);
    }

    /// <summary>Builds a data segment from plain text with the matching encoding.</summary>
    private static DataSegment CreateDataSegment(string plainText, bool forceUtf8, bool utf8Bom, ECIMode eciMode)
    {
        // Fast path: optimized Latin-1 when flags allow
        if (!forceUtf8 && !utf8Bom && eciMode == ECIMode.Default && OptimizedLatin1DataSegment.CanEncode(plainText))
            return new OptimizedLatin1DataSegment(plainText);

        var encoding = GetEncodingFromPlaintext(plainText, forceUtf8);

        // Segment type from encoding mode
        return encoding switch {
            EncodingMode.Numeric => new NumericDataSegment(plainText),
            EncodingMode.Alphanumeric => new AlphanumericDataSegment(plainText),
            EncodingMode.Byte => new ByteDataSegment(plainText, forceUtf8, utf8Bom, eciMode),
            var _ => throw new InvalidOperationException($"Unsupported encoding mode: {encoding}")
        };
    }

    /// <summary>
    /// Picks a QR version from the data segment and ECC level. Checks that a requested version is large enough, or finds the smallest version when none is given.
    /// </summary>
    private static int DetermineVersion(DataSegment segment, ECCLevel eccLevel, int version)
    {
        if (!CapacityTables.TryCalculateMinimumVersion(segment, eccLevel, out var minVersion))
            return Throw(eccLevel, segment.EncodingMode, version == -1 ? 40 : version);

        if (version == -1)
            return minVersion;

        // Fixed version from the caller; confirm it is large enough.
        if (minVersion > version) {
            // Throw-helper so the failure path does not allocate a closure
            return Throw(eccLevel, segment.EncodingMode, version);
        }

        return version;

        static int Throw(ECCLevel eccLevel, EncodingMode encoding, int version)
        {
            var maxSizeByte = CapacityTables.GetVersionInfo(version).Details.First(x => x.ErrorCorrectionLevel == eccLevel).CapacityDict[encoding];
            throw new DataTooLongException(eccLevel.ToString(), encoding.ToString(), version, maxSizeByte);
        }
    }

    /// <summary>Builds Micro QR data that a renderer can turn into a graphic.</summary>
    /// <param name="plainText">Payload to encode in the QR code</param>
    /// <param name="eccLevel">Error-correction level</param>
    /// <param name="requestedVersion">Fixed Micro QR version: -1 to -4 for M1 to M4, or 0 for default.</param>
    /// <exception cref="DataTooLongException">Raised when the payload is too large for a QR code.</exception>
    /// <returns>Raw QR data for rendering.</returns>
    public static QRIsoMatrix GenerateMicroQrCode(string plainText, ECCLevel eccLevel = ECCLevel.Default, int requestedVersion = 0)
    {
        if (requestedVersion is < -4 or > 0)
            throw new ArgumentOutOfRangeException(nameof(requestedVersion), requestedVersion, "Requested version must be -1 to -4 representing M1 to M4, or 0 for default.");

        ValidateECCLevel(eccLevel);
        if (eccLevel == ECCLevel.H)
            throw new ArgumentOutOfRangeException(nameof(eccLevel), eccLevel, "Micro QR codes does not support error correction level H.");

        if (eccLevel == ECCLevel.Q && requestedVersion == 0)
            requestedVersion = -4; // Q without a version → M4 (Q is only on M4)

        if (eccLevel == ECCLevel.Q && requestedVersion != -4)
            throw new ArgumentOutOfRangeException(nameof(eccLevel), eccLevel, "Micro QR codes only supports error correction level Q for version M4.");

        if (eccLevel != ECCLevel.Default && requestedVersion == -1)
            throw new ArgumentOutOfRangeException(nameof(eccLevel), eccLevel, "Please specify ECCLevel.Default for version M1.");

        if (plainText == null)
            throw new ArgumentNullException(nameof(plainText));

        var encoding = GetEncodingFromPlaintext(plainText, false);
        var codedText = PlainTextToBinary(plainText, encoding, ECIMode.Default, false, false);
        var dataInputLength = GetDataLength(encoding, plainText, codedText, false);
        var version = requestedVersion;
        var minVersion = CapacityTables.CalculateMinimumMicroVersion(dataInputLength, encoding, eccLevel);
        if (version == 0)
            version = minVersion;
        else {
            // Fixed version from the caller; confirm it is large enough.
            if (minVersion < version) {
                var matchedEncoding = CapacityTables.GetVersionInfo(version)
                    .Details.First(x => x.ErrorCorrectionLevel == eccLevel || (eccLevel == ECCLevel.Default && x.ErrorCorrectionLevel == ECCLevel.L))
                    .CapacityDict.TryGetValue(encoding, out var maxSizeByte);

                if (!matchedEncoding)
                    throw new InvalidOperationException("Required encoding is not supported for this version.");

                throw new DataTooLongException(eccLevel.ToString(), encoding.ToString(), version, maxSizeByte);
            }
        }

        if (version < -1 && eccLevel == ECCLevel.Default)
            eccLevel = ECCLevel.L;

        var modeIndicatorLength = -version - 1; // M1=0, M2=1, M3=2, M4=3
        var countIndicatorLength = GetCountIndicatorLength(version, encoding);
        var completeBitArrayLength = modeIndicatorLength + countIndicatorLength + codedText.Length;
        var completeBitArray = new BitArray(completeBitArrayLength);

        // mode indicator
        var completeBitArrayIndex = 0;
        if (version < 0) {
            var encodingValue = encoding == EncodingMode.Numeric ? 0 : encoding == EncodingMode.Alphanumeric ? 1 : encoding == EncodingMode.Byte ? 2 : 3;
            completeBitArrayIndex = DecToBin(encodingValue, modeIndicatorLength, completeBitArray, completeBitArrayIndex);
        }
        else
            completeBitArrayIndex = DecToBin((int)encoding, 4, completeBitArray, completeBitArrayIndex);

        // count indicator
        completeBitArrayIndex = DecToBin(dataInputLength, countIndicatorLength, completeBitArray, completeBitArrayIndex);
        // payload
        for (var i = 0; i < codedText.Length; i++)
            completeBitArray[completeBitArrayIndex++] = codedText[i];

        return GenerateQrCode(completeBitArray, eccLevel, version);
    }

    /// <summary>Builds QR data that a renderer can turn into a graphic.</summary>
    /// <param name="binaryData">Bytes to encode in the QR code</param>
    /// <param name="eccLevel">Error-correction level</param>
    /// <exception cref="DataTooLongException">Raised when the payload is too large for a QR code.</exception>
    /// <returns>Raw QR data for rendering.</returns>
    public static QRIsoMatrix GenerateQrCode(byte[] binaryData, ECCLevel eccLevel)
    {
        eccLevel = ValidateECCLevel(eccLevel);
        var version = CapacityTables.CalculateMinimumVersion(binaryData.Length, EncodingMode.Byte, eccLevel);
        var countIndicatorLen = GetCountIndicatorLength(version, EncodingMode.Byte);
        // Bytes to bits, with room at the front for mode and count indicators
        var bitArray = ToBitArray(binaryData, 4 + countIndicatorLen);
        // Mode and count indicators
        var index = DecToBin((int)EncodingMode.Byte, 4, bitArray, 0);
        DecToBin(binaryData.Length, countIndicatorLen, bitArray, index);
        return GenerateQrCode(bitArray, eccLevel, version);
    }

    /// <summary>
    /// Accepts a valid ECC level, or maps Default to M. Raises if the level is invalid.
    /// </summary>
    private static ECCLevel ValidateECCLevel(ECCLevel eccLevel)
        => eccLevel switch {
            ECCLevel.L or ECCLevel.M or ECCLevel.Q or ECCLevel.H => eccLevel,
            ECCLevel.Default => ECCLevel.M,
            var _ => throw new ArgumentOutOfRangeException(nameof(eccLevel), eccLevel, "Invalid error correction level.")
        };

    /// <summary>
    /// Builds a QR matrix from a BitArray, ECC level, and version. The BitArray is expected to already include count, encoding mode, and/or ECI mode.
    /// </summary>
    /// <param name="bitArray">
    /// Encoded payload bits. Must already include count, encoding mode, and/or ECI mode.
    /// </param>
    /// <param name="eccLevel">Error-correction level (how much damage can be recovered).</param>
    /// <param name="version">QR version (matrix size and complexity).</param>
    /// <returns>A QRISOMatrix with the full module grid, for rendering or analysis.</returns>
    private static QRIsoMatrix GenerateQrCode(BitArray bitArray, ECCLevel eccLevel, int version)
    {
        var eccInfo = CapacityTables.GetEccInfo(version, eccLevel);

        // Pad data codewords
        PadData();

        // ECC blocks
        var codeWordWithECC = CalculateECCBlocks();

        // Interleaved length
        var interleavedLength = CalculateInterleavedLength();

        // Interleave codewords
        var interleavedData = InterleaveData();

        // Place interleaved bits on the matrix
        var qrData = PlaceModules();
        CodewordBlock.ReturnList(codeWordWithECC);
        return qrData;

        // Repeat a pad pattern until the bit array is the required length
        void PadData()
        {
            var dataLength = eccInfo.TotalDataBits;
            var lengthDiff = dataLength - bitArray.Length;
            if (lengthDiff > 0) {
                // write index at the current end
                var index = bitArray.Length;
                // grow to the required length
                bitArray.Length = dataLength;
                // terminator/pad length
                var padLength = version switch {
                    > 0 => 4,
                    -1 => 3,
                    -2 => 5,
                    -3 => 7,
                    var _ => 9
                };

                // zero pad (or shorter if space is tight)
                index += padLength;
                // align to an 8-bit boundary
                if ((uint)index % 8 != 0)
                    index += 8 - (int)((uint)index % 8);

                // M1 and M3: leave the last 4 bits out of the repeating pad
                if (version == -1 || version == -3)
                    dataLength -= 4;

                // repeating pad pattern
                var repeatingPatternIndex = 0;
                while (index < dataLength) {
                    bitArray[index++] = RepeatingPattern[repeatingPatternIndex++];
                    if (repeatingPatternIndex >= RepeatingPattern.Length)
                        repeatingPatternIndex = 0;
                }
            }
        }

        List<CodewordBlock> CalculateECCBlocks()
        {
            List<CodewordBlock> codewordBlocks;
            // Generator polynomial from ECC word count.
            using (var generatorPolynom = CalculateGeneratorPolynom(eccInfo.ECCPerBlock)) {
                // ECC words per block
                codewordBlocks = CodewordBlock.GetList(eccInfo.BlocksInGroup1 + eccInfo.BlocksInGroup2);
                AddCodeWordBlocks(1, eccInfo.BlocksInGroup1, eccInfo.CodewordsInGroup1, 0, bitArray.Length, generatorPolynom);
                var offset = eccInfo.BlocksInGroup1 * eccInfo.CodewordsInGroup1 * 8;
                AddCodeWordBlocks(2, eccInfo.BlocksInGroup2, eccInfo.CodewordsInGroup2, offset, bitArray.Length - offset, generatorPolynom);
                return codewordBlocks;
            }

            void AddCodeWordBlocks(int blockNum, int blocksInGroup, int codewordsInGroup, int offset2, int count, Polynom generatorPolynom)
            {
                _ = blockNum;
                var groupLength = codewordsInGroup * 8;
                groupLength = groupLength > count ? count : groupLength;
                for (var i = 0; i < blocksInGroup; i++) {
                    var eccWordList = CalculateECCWords(bitArray, offset2, groupLength, eccInfo, generatorPolynom);
                    codewordBlocks.Add(new(offset2, groupLength, eccWordList));
                    offset2 += groupLength;
                }
            }
        }

        // Interleaved payload length
        int CalculateInterleavedLength()
        {
            var length = 0;
            var codewords = Math.Max(eccInfo.CodewordsInGroup1, eccInfo.CodewordsInGroup2);
            if (version == -1 || version == -3) {
                codewords--;
                length += 4;
            }

            for (var i = 0; i < codewords; i++) {
                foreach (var codeBlock in codeWordWithECC) {
                    if ((uint)codeBlock.CodeWordsLength / 8 > i)
                        length += 8;
                }
            }

            for (var i = 0; i < eccInfo.ECCPerBlock; i++) {
                foreach (var codeBlock in codeWordWithECC) {
                    if (codeBlock.ECCWords.Count > i)
                        length += 8;
                }
            }

            length += CapacityTables.GetRemainderBits(version);
            return length;
        }

        // Interleave codewords
        BitArray InterleaveData()
        {
            var data = new BitArray(interleavedLength);
            var pos = 0;
            var codewords = Math.Max(eccInfo.CodewordsInGroup1, eccInfo.CodewordsInGroup2);
            if (version == -1 || version == -3)
                codewords--;

            for (var i = 0; i < Math.Max(eccInfo.CodewordsInGroup1, eccInfo.CodewordsInGroup2); i++) {
                foreach (var codeBlock in codeWordWithECC) {
                    if ((uint)codeBlock.CodeWordsLength / 8 > i)
                        pos = bitArray.CopyTo(data, (int)((uint)i * 8) + codeBlock.CodeWordsOffset, pos, 8);
                }
            }

            if (version == -1 || version == -3)
                pos = bitArray.CopyTo(data, (int)((uint)codewords * 8) + codeWordWithECC[0].CodeWordsOffset, pos, 4);

            for (var i = 0; i < eccInfo.ECCPerBlock; i++) {
                foreach (var codeBlock in codeWordWithECC) {
                    if (codeBlock.ECCWords.Count > i)
                        pos = DecToBin(codeBlock.ECCWords.Array![i], 8, data, pos);
                }
            }

            return data;
        }

        // Place modules on the matrix
        QRIsoMatrix PlaceModules()
        {
            var qr = new QRIsoMatrix(version, true);
            var size = qr.ModuleMatrix.Count - 8;
            var tempBitArray = new BitArray(18); // version string is 18 bits
            using (var blockedModules = new ModulePlacer.BlockedModules(size)) {
                ModulePlacer.PlaceFinderPatterns(qr, blockedModules);
                ModulePlacer.ReserveSeperatorAreas(version, size, blockedModules);
                ModulePlacer.PlaceAlignmentPatterns(qr, AlignmentPatterns.FromVersion(version).PatternPositions, blockedModules);
                ModulePlacer.PlaceTimingPatterns(qr, blockedModules);
                ModulePlacer.PlaceDarkModule(qr, version, blockedModules);
                ModulePlacer.ReserveVersionAreas(size, version, blockedModules);
                ModulePlacer.PlaceDataWords(qr, interleavedData, blockedModules);
                var maskVersion = ModulePlacer.MaskCode(qr, version, blockedModules, eccLevel);
                GetFormatString(tempBitArray, version, eccLevel, maskVersion);
                ModulePlacer.PlaceFormat(qr, tempBitArray, true);
            }

            if (version >= 7) {
                GetVersionString(tempBitArray, version);
                ModulePlacer.PlaceVersion(qr, tempBitArray, true);
            }

            return qr;
        }
    }

    /// <summary>
    /// Writes the 15-bit format string: ECC level, mask pattern, and format ECC bits.
    /// </summary>
    /// <param name="fStrEcc"><see cref="BitArray" /> to write into, or null to allocate one.</param>
    /// <param name="version">QR version (1-40, or -1 to -4 for Micro QR).</param>
    /// <param name="level">ECC level encoded in the format string.</param>
    /// <param name="maskVersion">Mask pattern encoded in the format string.</param>
    /// <returns>15-bit format string used during QR generation.</returns>
    private static void GetFormatString(BitArray fStrEcc, int version, ECCLevel level, int maskVersion)
    {
        fStrEcc.Length = 15;
        fStrEcc.SetAll(false);
        if (version < 0)
            WriteMicroEccLevelAndVersion();
        else
            WriteEccLevelAndVersion();

        // Format generator polynomial adds ECC to the format string.
        var index = 0;
        var count = 15;
        TrimLeadingZeros(fStrEcc, ref index, ref count);
        while (count > 10) {
            for (var i = 0; i < GetFormatGenerator.Length; i++)
                fStrEcc[index + i] ^= GetFormatGenerator[i];

            TrimLeadingZeros(fStrEcc, ref index, ref count);
        }

        // Align bits to the start of the array.
        ShiftTowardsBit0(fStrEcc, index);

        // Prefix ECC bits with ECC level and version.
        fStrEcc.Length = 10 + 5;
        ShiftAwayFromBit0(fStrEcc, 10 - count + 5);
        if (version < 0)
            WriteMicroEccLevelAndVersion();
        else
            WriteEccLevelAndVersion();

        // XOR with a fixed mask so format bits are more robust.
        fStrEcc.Xor(version < 0 ? GetFormatMicroMask : GetFormatMask);

        void WriteEccLevelAndVersion()
        {
            switch (level) {
                case ECCLevel.L: // bits 01
                    fStrEcc[1] = true;
                    break;
                case ECCLevel.H: // bits 10
                    fStrEcc[0] = true;
                    break;
                case ECCLevel.Q: // bits 11
                    fStrEcc[0] = true;
                    fStrEcc[1] = true;
                    break;
            }

            // 3-bit mask version right after the ECC-level bits.
            DecToBin(maskVersion, 3, fStrEcc, 2);
        }

        void WriteMicroEccLevelAndVersion()
        {
            switch (version) {
                case -1: // version M1
                    break;
                case -2: // version M2
                    fStrEcc[level == ECCLevel.L ? 2 : 1] = true; // L=001, M=010
                    break;
                case -3: // version M3
                    if (level == ECCLevel.L) {
                        fStrEcc[1] = true; // L=011
                        fStrEcc[2] = true;
                    }
                    else
                        fStrEcc[0] = true; // M=100

                    break;
                default: // version M4
                    fStrEcc[0] = true;
                    if (level == ECCLevel.L) // L=101
                        fStrEcc[2] = true;
                    else if (level == ECCLevel.M) // M=110
                        fStrEcc[1] = true;
                    else // Q=111
                    {
                        fStrEcc[1] = true;
                        fStrEcc[2] = true;
                    }

                    break;
            }

            // 2-bit mask version right after version / ECC-level bits.
            var microMaskVersion = maskVersion switch {
                1 => 0,
                4 => 1,
                6 => 2,
                var _ => 3
            };

            DecToBin(microMaskVersion, 2, fStrEcc, 3);
        }
    }

#if !NETFRAMEWORK || NET45_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private static void TrimLeadingZeros(BitArray fStrEcc, ref int index, ref int count)
    {
        while (count > 0 && !fStrEcc[index]) {
            index++;
            count--;
        }
    }

    private static void ShiftTowardsBit0(BitArray fStrEcc, int num)
    {
#if HAS_SPAN
        fStrEcc.RightShift(num); // toward bit 0
#else
        for (var i = 0; i < fStrEcc.Length - num; i++)
            fStrEcc[i] = fStrEcc[i + num];
        for (var i = fStrEcc.Length - num; i < fStrEcc.Length; i++)
            fStrEcc[i] = false;
#endif
    }

    private static void ShiftAwayFromBit0(BitArray fStrEcc, int num)
    {
#if HAS_SPAN
        fStrEcc.LeftShift(num); // away from bit 0
#else
        for (var i = fStrEcc.Length - 1; i >= num; i--)
            fStrEcc[i] = fStrEcc[i - num];
        for (var i = 0; i < num; i++)
            fStrEcc[i] = false;
#endif
    }

    /// <summary>
    /// Encodes version information into a BitArray with ECC similar to format encoding. Used for QR versions 7 and above.
    /// </summary>
    /// <param name="vStr"><see cref="BitArray" /> that receives the version string.</param>
    /// <param name="version">QR version (7-40).</param>
    /// <returns>Encoded version bits, including ECC.</returns>
    private static void GetVersionString(BitArray vStr, int version)
    {
        vStr.Length = 18;
        vStr.SetAll(false);
        DecToBin(version, 6, vStr, 0); // version as 6 bits
        var count = vStr.Length;
        var index = 0;
        TrimLeadingZeros(vStr, ref index, ref count); // drop leading zeros so the sequence is normalized

        // Version ECC via the version generator polynomial.
        while (count > 12) // version ECC target is 12 bits
        {
            for (var i = 0; i < GetVersionGenerator.Length; i++)
                vStr[index + i] ^= GetVersionGenerator[i]; // XOR with the generator

            TrimLeadingZeros(vStr, ref index, ref count); // drop leading zeros after each XOR
        }

        ShiftTowardsBit0(vStr, index); // data starts at index 0

        // Prefix ECC with 6 version bits
        vStr.Length = 12 + 6;
        ShiftAwayFromBit0(vStr, 12 - count + 6);
        DecToBin(version, 6, vStr, 0);
    }

    /// <summary>
    /// ECC codewords for a data slice. Divides the message polynomial by the generator polynomial; the remainder is the ECC.
    /// </summary>
    private static ArraySegment<byte> CalculateECCWords(BitArray bitArray, int offset, int count, ECCInfo eccInfo, Polynom generatorPolynomBase)
    {
        var eccWords = eccInfo.ECCPerBlock;
        // Message polynomial from the bit slice.
        var messagePolynom = CalculateMessagePolynom(bitArray, offset, count);
        var generatorPolynom = generatorPolynomBase.Clone();

        // Raise message exponents by the ECC length.
        for (var i = 0; i < messagePolynom.Count; i++)
            messagePolynom[i] = new(messagePolynom[i].Coefficient, messagePolynom[i].Exponent + eccWords);

        // Raise generator exponents from the message degree.
        for (var i = 0; i < generatorPolynom.Count; i++)
            generatorPolynom[i] = new(generatorPolynom[i].Coefficient, generatorPolynom[i].Exponent + (messagePolynom.Count - 1));

        // Remainder of message / generator.
        var leadTermSource = messagePolynom;
        for (var i = 0; leadTermSource.Count > 0 && leadTermSource[^1].Exponent > 0; i++) {
            if (leadTermSource[0].Coefficient == 0) // drop a leading zero coefficient
            {
                leadTermSource.RemoveAt(0);
                leadTermSource.Add(new(0, leadTermSource[^1].Exponent - 1));
            }
            else // XOR-reduce with the generator
            {
                // First coefficient to alpha exponent; zero stays zero (log(0) is undefined).
                var index0Coefficient = leadTermSource[0].Coefficient;
                index0Coefficient = index0Coefficient == 0 ? 0 : GaloisField.GetAlphaExpFromIntVal(index0Coefficient);
                var alphaNotation = new PolynomItem(index0Coefficient, leadTermSource[0].Exponent);
                var resPoly = MultiplyGeneratorPolynomByLeadterm(generatorPolynom, alphaNotation, i);
                ConvertToDecNotationInPlace(resPoly);
                var newPoly = XORPolynoms(leadTermSource, resPoly);
                // Release the previous polynomials.
                resPoly.Dispose();
                leadTermSource.Dispose();
                // Remainder becomes the next message polynomial.
                leadTermSource = newPoly;
            }
        }

        // Release the generator polynomial.
        generatorPolynom.Dispose();

        // Remainder coefficients as ECC bytes.
#if HAS_SPAN
        var array = ArrayPool<byte>.Shared.Rent(leadTermSource.Count);
        var ret = new ArraySegment<byte>(array, 0, leadTermSource.Count);
#else
        var ret = new ArraySegment<byte>(new byte[leadTermSource.Count]);
        var array = ret.Array!;
#endif
        for (var i = 0; i < leadTermSource.Count; i++)
            array[i] = (byte)leadTermSource[i].Coefficient;

        // Release the message polynomial.
        leadTermSource.Dispose();
        return ret;
    }

    /// <summary>
    /// Converts each coefficient from alpha-exponent form to decimal in place, for operations that need integer coefficients.
    /// </summary>
    private static void ConvertToDecNotationInPlace(Polynom poly)
    {
        for (var i = 0; i < poly.Count; i++)
            // Alpha exponent → decimal coefficient.
            poly[i] = new(GaloisField.GetIntValFromAlphaExp(poly[i].Coefficient), poly[i].Exponent);
    }

    /// <summary>Best encoding mode for the text, from its characters and the force-UTF-8 flag.</summary>
    internal static EncodingMode GetEncodingFromPlaintext(string plainText, bool forceUtf8)
    {
        if (forceUtf8)
            return EncodingMode.Byte;

        var result = EncodingMode.Numeric; // start as numeric
        foreach (var c in plainText) {
            if (IsInRange(c, '0', '9'))
                continue; // digit (Latin-1, not char.IsDigit)

            result = EncodingMode.Alphanumeric; // not numeric; try alphanumeric
            if (AlphanumericEncoder.CanEncode(c))
                continue; // alphanumeric

            return EncodingMode.Byte; // neither numeric nor alphanumeric
        }

        return result; // numeric or alphanumeric
    }

    /// <summary>True if the character is in the inclusive range.</summary>
    private static bool IsInRange(char c, char min, char max) => (uint)(c - min) <= (uint)(max - min);

    /// <summary>Builds a message polynomial from a BitArray slice, padding the last byte when needed (Micro QR M1/M3).</summary>
    /// <param name="bitArray">Encoded QR bits.</param>
    /// <param name="offset">Start index in the bit array.</param>
    /// <param name="bitCount">Bits to convert into codewords.</param>
    /// <returns>Polynomial of the message codewords.</returns>
    private static Polynom CalculateMessagePolynom(BitArray bitArray, int offset, int bitCount)
    {
        // Full 8-bit codewords
        var fullBytes = bitCount / 8;

        // Leftover bits (e.g. 4 bits on Micro QR M1/M3)
        var remainingBits = bitCount % 8;
        if (remainingBits > 0) {
            // Zero-pad the last byte to 8 bits
            var addlBits = 8 - remainingBits;
            var minBitArrayLength = offset + bitCount + addlBits;

            // Grow the BitArray if the pad does not fit
            if (bitArray.Length < minBitArrayLength)
                bitArray.Length = minBitArrayLength;

            // Pad remaining bits with 0
            for (var i = 0; i < addlBits; i++)
                bitArray[offset + bitCount + i] = false;
        }

        // Codeword count (includes a partial last byte when present)
        var polynomLength = fullBytes + (remainingBits > 0 ? 1 : 0);

        // Message polynomial
        var messagePol = new Polynom(polynomLength);

        // Highest-degree exponent first
        var exponent = polynomLength - 1;

        // Each 8-bit group as a decimal term
        for (var i = 0; i < polynomLength; i++) {
            messagePol.Add(new(BinToDec(bitArray, offset, 8), exponent--));
            offset += 8;
        }

        return messagePol;
    }

    /// <summary>Generator polynomial used to build ECC codewords.</summary>
    /// <param name="numEccWords">Number of ECC codewords to generate.</param>
    /// <returns>Polynomial used to generate ECC codewords.</returns>
    private static Polynom CalculateGeneratorPolynom(int numEccWords)
    {
        var generatorPolynom = new Polynom(2); // simplest generator seed
        generatorPolynom.Add(new(0, 1));
        generatorPolynom.Add(new(0, 0));
        using (var multiplierPolynom = new Polynom(numEccWords * 2)) // scratch for each multiply
        {
            for (var i = 1; i <= numEccWords - 1; i++) {
                // Reset the multiplier for this step
                multiplierPolynom.Clear();
                multiplierPolynom.Add(new(0, 1));
                multiplierPolynom.Add(new(i, 0));

                // generator *= multiplier
                var newGeneratorPolynom = MultiplyAlphaPolynoms(generatorPolynom, multiplierPolynom);
                generatorPolynom.Dispose();
                generatorPolynom = newGeneratorPolynom;
            }
        }

        return generatorPolynom; // finished generator
    }

    /// <summary>Integer value of a BitArray slice.</summary>
    /// <returns>Integer for the given bits.</returns>
    private static int BinToDec(BitArray bitArray, int offset, int count)
    {
        var ret = 0;
        for (var i = 0; i < count; i++)
            ret ^= bitArray[offset + i] ? 1 << (count - i - 1) : 0;

        return ret;
    }

    /// <summary>Writes a decimal value as a fixed-width binary field into a BitArray.</summary>
    /// <param name="decNum">Decimal value to convert.</param>
    /// <param name="bits">Bit width (e.g. 8, 16, 32).</param>
    /// <param name="bitList">BitArray that receives the bits.</param>
    /// <param name="index">Start index in the BitArray.</param>
    /// <returns>Next index after the last bit written.</returns>
    private static int DecToBin(int decNum, int bits, BitArray bitList, int index)
    {
        // Bitwise conversion of decNum
        for (var i = bits - 1; i >= 0; i--) {
            // MSB to LSB
            var bit = (decNum & (1 << i)) != 0;
            bitList[index++] = bit;
        }

        return index;
    }

    /// <summary>Count-indicator bit width for a version and encoding mode.</summary>
    /// <param name="version">QR version (larger versions use more count bits).</param>
    /// <param name="encMode">Encoding mode (e.g., Numeric, Alphanumeric, Byte).</param>
    /// <returns>Bits needed for the character count at that mode and version.</returns>
    private static int GetCountIndicatorLength(int version, EncodingMode encMode)
    {
        // Count-indicator width depends on version and mode
        if (version == -1)
            return 3;

        if (version == -2)
            return encMode == EncodingMode.Numeric ? 4 : 3;

        if (version == -3) {
            if (encMode == EncodingMode.Numeric)
                return 5;

            if (encMode == EncodingMode.Kanji)
                return 3;

            return 4;
        }

        if (version == -4) {
            if (encMode == EncodingMode.Numeric)
                return 6;

            if (encMode == EncodingMode.Kanji)
                return 4;

            return 5;
        }

        if (version < 10) {
            if (encMode == EncodingMode.Numeric)
                return 10;

            if (encMode == EncodingMode.Alphanumeric)
                return 9;

            return 8;
        }

        if (version < 27) {
            if (encMode == EncodingMode.Numeric)
                return 12;

            if (encMode == EncodingMode.Alphanumeric)
                return 11;

            if (encMode == EncodingMode.Byte)
                return 16;

            return 10;
        }

        if (encMode == EncodingMode.Numeric)
            return 14;

        if (encMode == EncodingMode.Alphanumeric)
            return 13;

        if (encMode == EncodingMode.Byte)
            return 16;

        return 12;
    }

    /// <summary>Data length from encoding mode, text, and the force-UTF-8 flag.</summary>
    /// <param name="encoding">Encoding mode used for the QR data.</param>
    /// <param name="plainText">Plain text to encode.</param>
    /// <param name="codedText">Encoded bits of the text.</param>
    /// <param name="forceUtf8">If true, treat the payload as UTF-8 bytes.</param>
    /// <returns>Length in bytes or characters, depending on the encoding.</returns>
    private static int GetDataLength(EncodingMode encoding, string plainText, BitArray codedText, bool forceUtf8)
    {
        // UTF-8 (forced or detected) → byte count; otherwise character count.
        return forceUtf8 || IsUtf8() ? (int)((uint)codedText.Length / 8) : plainText.Length;

        bool IsUtf8() => encoding == EncodingMode.Byte && (forceUtf8 || !IsValidISO(plainText));
    }

    /// <summary>True if the string round-trips in ISO-8859-1.</summary>
    private static bool IsValidISO(string input)
        =>
            // ISO-8859-1 matches UTF-16 in 0x00-0xFF.
            //   0x00-0x7F: ASCII (0-127)
            //   0x80-0x9F: C1 controls (128-159)
            //   0xA0-0xFF: Extended Latin (160-255)
            input.All(c => c <= 0xFF);

    /// <summary>Encodes plain text as QR bits for the given encoding mode.</summary>
    /// <param name="plainText">Text to encode.</param>
    /// <param name="encMode">Encoding mode.</param>
    /// <param name="eciMode">ECI mode that selects the character encoding.</param>
    /// <param name="utf8Bom">If true, prepend a UTF-8 BOM.</param>
    /// <param name="forceUtf8">If true, force UTF-8 encoding.</param>
    /// <returns>BitArray of the encoded payload.</returns>
    private static BitArray PlainTextToBinary(string plainText, EncodingMode encMode, ECIMode eciMode, bool utf8Bom, bool forceUtf8)
        => encMode switch {
            EncodingMode.Alphanumeric => AlphanumericEncoder.GetBitArray(plainText),
            EncodingMode.Numeric => PlainTextToBinaryNumeric(plainText),
            EncodingMode.Byte => PlainTextToBinaryByte(plainText, eciMode, utf8Bom, forceUtf8),
            var _ => EmptyBitArray
        };

    /// <summary>
    /// Bytes to BitArray, keeping MSB-to-LSB order inside each byte (unlike the BitArray constructor).
    /// </summary>
    /// <param name="byteArray">Bytes to convert.</param>
    /// <param name="prefixZeros">Leading zeros to prepend.</param>
    /// <returns>BitArray of the input bits, plus optional leading zeros.</returns>
    private static BitArray ToBitArray(
#if HAS_SPAN
        ReadOnlySpan<byte> byteArray, // byte[] converts to ReadOnlySpan<byte>
#else
        byte[] byteArray,
#endif
        int prefixZeros = 0)
    {
        // Total bits including prefix zeros.
        var bitArray = new BitArray((int)((uint)byteArray.Length * 8) + prefixZeros);
        CopyToBitArray(byteArray, bitArray, prefixZeros);
        return bitArray;
    }

    /// <summary>
    /// Copies bytes into a BitArray at an offset, keeping MSB-to-LSB order inside each byte (unlike the BitArray constructor).
    /// </summary>
    /// <param name="byteArray">Bytes to copy.</param>
    /// <param name="bitArray">Target BitArray.</param>
    /// <param name="offset">Start offset in the BitArray.</param>
    private static void CopyToBitArray(
#if HAS_SPAN
        ReadOnlySpan<byte> byteArray, // byte[] converts to ReadOnlySpan<byte>
#else
        byte[] byteArray,
#endif
        BitArray bitArray,
        int offset)
    {
        for (var i = 0; i < byteArray.Length; i++) {
            var byteVal = byteArray[i];
            for (var j = 0; j < 8; j++)
                // Copy each bit, shifting so MSB is first.
                bitArray[(int)((uint)i * 8) + j + offset] = (byteVal & (1 << (7 - j))) != 0;
        }
    }

    /// <summary>Bitwise XOR of two polynomials, used in QR ECC.</summary>
    /// <returns>Polynomial after the XOR.</returns>
    private static Polynom XORPolynoms(Polynom messagePolynom, Polynom resPolynom)
    {
        // Longer polynomial drives the XOR.
        var resultPolynom = new Polynom(Math.Max(messagePolynom.Count, resPolynom.Count) - 1);
        Polynom longPoly, shortPoly;
        if (messagePolynom.Count >= resPolynom.Count) {
            longPoly = messagePolynom;
            shortPoly = resPolynom;
        }
        else {
            longPoly = resPolynom;
            shortPoly = messagePolynom;
        }

        // XOR coefficients.
        for (var i = 1; i < longPoly.Count; i++) {
            var polItemRes = new PolynomItem(longPoly[i].Coefficient ^ (shortPoly.Count > i ? shortPoly[i].Coefficient : 0), messagePolynom[0].Exponent - i);
            resultPolynom.Add(polItemRes);
        }

        return resultPolynom;
    }

    /// <summary>
    /// Multiplies the generator polynomial by a lead term and lowers exponents. Used when building QR ECC codewords.
    /// </summary>
    private static Polynom MultiplyGeneratorPolynomByLeadterm(Polynom genPolynom, PolynomItem leadTerm, int lowerExponentBy)
    {
        var resultPolynom = new Polynom(genPolynom.Count);
        foreach (var polItemBase in genPolynom) {
            var polItemRes = new PolynomItem((polItemBase.Coefficient + leadTerm.Coefficient) % 255, polItemBase.Exponent - lowerExponentBy);
            resultPolynom.Add(polItemRes);
        }

        return resultPolynom;
    }

    /// <summary>Multiplies two polynomials whose coefficients are alpha exponents (Reed-Solomon style).</summary>
    /// <param name="polynomBase">First polynomial.</param>
    /// <param name="polynomMultiplier">Second polynomial.</param>
    /// <returns>Product of the two polynomials.</returns>
    private static Polynom MultiplyAlphaPolynoms(Polynom polynomBase, Polynom polynomMultiplier)
    {
        // Room for every product term.
        var resultPolynom = new Polynom(polynomMultiplier.Count * polynomBase.Count);

        // Every term of the first times every term of the second.
        foreach (var polItemBase in polynomMultiplier) {
            foreach (var polItemMulti in polynomBase) {
                // Add alpha exponents; sum the polynomial exponents.
                var polItemRes = new PolynomItem(GaloisField.ShrinkAlphaExp(polItemBase.Coefficient + polItemMulti.Coefficient), polItemBase.Exponent + polItemMulti.Exponent);
                resultPolynom.Add(polItemRes);
            }
        }

        // Merge like exponents.
#if NET5_0_OR_GREATER
        var toGlue = GetNotUniqueExponents(resultPolynom, resultPolynom.Count <= 128 ? stackalloc int[128].Slice(0, resultPolynom.Count) : new int[resultPolynom.Count]);
        var gluedPolynoms = toGlue.Length <= 128 ? stackalloc PolynomItem[128].Slice(0, toGlue.Length) : new PolynomItem[toGlue.Length];
#else
        var toGlue = GetNotUniqueExponents(resultPolynom);
        var gluedPolynoms = new PolynomItem[toGlue.Length];
#endif
        var gluedPolynomsIndex = 0;
        foreach (var exponent in toGlue) {
            var coefficient = 0;
            foreach (var polynomOld in resultPolynom) {
                if (polynomOld.Exponent == exponent)
                    coefficient ^= GaloisField.GetIntValFromAlphaExp(polynomOld.Coefficient);
            }

            // Rebuild coefficients from the XOR of like terms.
            var polynomFixed = new PolynomItem(GaloisField.GetAlphaExpFromIntVal(coefficient), exponent);
            gluedPolynoms[gluedPolynomsIndex++] = polynomFixed;
        }

        // Drop duplicate exponents, then add the merged terms.
        for (var i = resultPolynom.Count - 1; i >= 0; i--)
#if NET5_0_OR_GREATER
        {
            if (toGlue.Contains(resultPolynom[i].Exponent))
#else
            if (Array.IndexOf(toGlue, resultPolynom[i].Exponent) >= 0)
#endif
                resultPolynom.RemoveAt(i);
        }

        foreach (var polynom in gluedPolynoms)
            resultPolynom.Add(polynom);

        // Highest exponent first.
        resultPolynom.Sort((x, y) => -x.Exponent.CompareTo(y.Exponent));
        return resultPolynom;

        // Exponents that appear more than once.
#if NET5_0_OR_GREATER
        static ReadOnlySpan<int> GetNotUniqueExponents(Polynom list, Span<int> buffer)
        {
            // Steps:
            // 1. caller passes a scratch buffer the same size as the list
            // 2. copy exponents into that buffer
            // 3. sort the buffer
            // 4. walk the sorted exponents and compare to the previous
            //   * equal → increment a counter
            //   * else if the counter is > 0, write the exponent to the result
            //
            // The result is written back into the same scratch buffer. The write index is always
            // <= the read index, so the two sides do not overlap.
            Debug.Assert(list.Count == buffer.Length);
            var idx = 0;
            foreach (var row in list)
                buffer[idx++] = row.Exponent;

            buffer.Sort();
            idx = 0;
            var expCount = 0;
            var last = buffer[0];
            for (var i = 1; i < buffer.Length; ++i) {
                if (buffer[i] == last)
                    expCount++;
                else {
                    if (expCount > 0) {
                        Debug.Assert(idx <= i - 1);
                        buffer[idx++] = last;
                        expCount = 0;
                    }
                }

                last = buffer[i];
            }

            return buffer.Slice(0, idx);
        }
#else
        static int[] GetNotUniqueExponents(Polynom list)
        {
            var dic = new Dictionary<int, bool>(list.Count);
            foreach (var row in list)
            {
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                if (!dic.TryAdd(row.Exponent, false))
#else
                if (!dic.ContainsKey(row.Exponent))
                    dic.Add(row.Exponent, false);
                else
#endif
                    dic[row.Exponent] = true;
            }

            // Exponents that appeared more than once.
            int count = 0;
            foreach (var row in dic)
            {
                if (row.Value)
                    count++;
            }

            var result = new int[count];
            int i = 0;
            foreach (var row in dic)
            {
                if (row.Value)
                    result[i++] = row.Key;
            }

            return result;
        }
#endif
    }
}