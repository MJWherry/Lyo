namespace Lyo.QRCode.Encoding.Iso;

internal sealed partial class QRIsoEncoder
{
    private static partial class ModulePlacer
    {
        /// <summary>
        /// Writes version information for versions 7 and higher into two small rectangles near the bottom-left and top-right corners, outside the timing patterns.
        /// </summary>
        /// <param name="qrCode">QR matrix to update.</param>
        /// <param name="versionStr">Bits of the version information.</param>
        /// <param name="offset">If true, apply an offset when placing version information.</param>
        public static void PlaceVersion(QRIsoMatrix qrCode, BitArray versionStr, bool offset)
        {
            var offsetValue = offset ? 4 : 0;
            var size = qrCode.ModuleMatrix.Count - offsetValue - offsetValue;

            // Version-info modules next to the separators.
            for (var x = 0; x < 6; x++) {
                for (var y = 0; y < 3; y++) {
                    // Map versionStr bits onto the matching modules.
                    qrCode.ModuleMatrix[y + size - 11 + offsetValue][x + offsetValue] = versionStr[17 - (x * 3 + y)];
                    qrCode.ModuleMatrix[x + offsetValue][y + size - 11 + offsetValue] = versionStr[17 - (x * 3 + y)];
                }
            }
        }

        /// <summary>Writes format information: error-correction level and mask pattern.</summary>
        /// <param name="qrCode">QR matrix to update.</param>
        /// <param name="formatStr">Bits of the format information.</param>
        /// <param name="offset">If true, apply an offset.</param>
        public static void PlaceFormat(QRIsoMatrix qrCode, BitArray formatStr, bool offset)
        {
            var isMicro = qrCode.Version < 0; // negative versions are Micro QR
            var offsetValue = offset ? 4 : 0;
            var size = qrCode.ModuleMatrix.Count - offsetValue - offsetValue;

            // Standard QR format positions:
            //
            //    { x1, y1, x2, y2 }          i
            //    ===============================
            //    { 8, 0, size - 1, 8 },   // 0
            //    { 8, 1, size - 2, 8 },   // 1
            //    { 8, 2, size - 3, 8 },   // 2
            //    { 8, 3, size - 4, 8 },   // 3
            //    { 8, 4, size - 5, 8 },   // 4
            //    { 8, 5, size - 6, 8 },   // 5
            //    { 8, 7, size - 7, 8 },   // 6
            //    { 8, 8, size - 8, 8 },   // 7
            //    { 7, 8, 8, size - 7 },   // 8
            //    { 5, 8, 8, size - 6 },   // 9
            //    { 4, 8, 8, size - 5 },   // 10
            //    { 3, 8, 8, size - 4 },   // 11
            //    { 2, 8, 8, size - 3 },   // 12
            //    { 1, 8, 8, size - 2 },   // 13
            //    { 0, 8, 8, size - 1 } }; // 14

            // Micro QR format positions:
            //
            //     i    { x1, y1 }
            //     ===============
            //     0    { 8, 1 }
            //     1    { 8, 2 }
            //     2    { 8, 3 }
            //     3    { 8, 4 }
            //     4    { 8, 5 }
            //     5    { 8, 6 }
            //     6    { 8, 7 }
            //     7    { 8, 8 }
            //     8    { 7, 8 }
            //     9    { 6, 8 }
            //     10   { 5, 8 }
            //     11   { 4, 8 }
            //     12   { 3, 8 }
            //     13   { 2, 8 }
            //     14   { 1, 8 }

            // The bit pattern is one word; LSB is at position 0.
            // Reverse the generated pattern, hence (14 - i) below.
            for (var i = 0; i < 15; i++) {
                int x1, y1, x2, y2;
                if (isMicro) {
                    // Micro QR format coords
                    x1 = i < 8 ? 8 : 14 - i + 1;
                    y1 = i < 8 ? i + 1 : 8;

                    // Micro QR writes format once; no second copy.
                    qrCode.ModuleMatrix[y1 + offsetValue][x1 + offsetValue] = formatStr[14 - i];
                }
                else {
                    // Standard QR format coords
                    x1 = i < 8 ? 8 : i == 8 ? 7 : 14 - i;
                    y1 = i < 6 ? i : i < 7 ? i + 1 : 8;
                    x2 = i < 8 ? size - 1 - i : 8;
                    y2 = i < 8 ? 8 : size - (15 - i);
                    qrCode.ModuleMatrix[y1 + offsetValue][x1 + offsetValue] = formatStr[14 - i];
                    qrCode.ModuleMatrix[y2 + offsetValue][x2 + offsetValue] = formatStr[14 - i];
                }
            }
        }

        /// <summary>Applies the mask with the lowest penalty score so scanners can read the symbol more reliably.</summary>
        /// <param name="qrCode">QR matrix to mask.</param>
        /// <param name="version">QR version (size and complexity).</param>
        /// <param name="blockedModules">Rectangles that must not be overwritten.</param>
        /// <param name="eccLevel">Error-correction level; used in the format string.</param>
        /// <returns>Index of the chosen mask pattern.</returns>
        public static int MaskCode(QRIsoMatrix qrCode, int version, BlockedModules blockedModules, ECCLevel eccLevel)
        {
            var selectedPattern = -1; // none chosen yet
            var patternScore = int.MaxValue; // lower is better
            var size = qrCode.ModuleMatrix.Count - 8;

            // Scratch matrix so trial masks do not change the original.
            var qrTemp = new QRIsoMatrix(version, false);
            BitArray? versionString = null;
            if (version >= 7) {
                versionString = new(18);
                GetVersionString(versionString, version);
            }

            var formatStr = new BitArray(15);
            for (var maskPattern = 0; maskPattern < 8; maskPattern++) {
                if (version < 0 && (maskPattern == 0 || maskPattern == 2 || maskPattern == 3 || maskPattern == 5))
                    continue; // Micro QR allows only some mask patterns.

                var patternFunc = MaskPattern.Patterns[maskPattern];

                // Copy the live matrix into the scratch matrix.
                for (var y = 0; y < size; y++) {
                    for (var x = 0; x < size; x++)
                        qrTemp.ModuleMatrix[y][x] = qrCode.ModuleMatrix[y + 4][x + 4];
                }

                // Format bits for this mask.
                GetFormatString(formatStr, version, eccLevel, maskPattern);
                PlaceFormat(qrTemp, formatStr, false);

                // Version bits when required.
                if (versionString != null) // aka if (version >= 7)
                    PlaceVersion(qrTemp, versionString, false);

                // Mask and score.
                for (var x = 0; x < size; x++) {
                    for (var y = 0; y < x; y++) {
                        if (!blockedModules.IsBlocked(x, y)) {
                            qrTemp.ModuleMatrix[y][x] ^= patternFunc(x, y);
                            qrTemp.ModuleMatrix[x][y] ^= patternFunc(y, x);
                        }
                    }

                    if (!blockedModules.IsBlocked(x, x))
                        qrTemp.ModuleMatrix[x][x] ^= patternFunc(x, x);
                }

                var score = version < 0 ? MaskPattern.ScoreMicro(qrTemp) : MaskPattern.Score(qrTemp);

                // Keep the lowest-scoring (most readable) pattern.
                if (patternScore > score) {
                    selectedPattern = maskPattern;
                    patternScore = score;
                }
            }

            // Apply the winning mask to the live matrix.
            var selectedPatternFunc = MaskPattern.Patterns[selectedPattern];
            for (var x = 0; x < size; x++) {
                for (var y = 0; y < x; y++) {
                    if (!blockedModules.IsBlocked(x, y)) {
                        qrCode.ModuleMatrix[y + 4][x + 4] ^= selectedPatternFunc(x, y);
                        qrCode.ModuleMatrix[x + 4][y + 4] ^= selectedPatternFunc(y, x);
                    }
                }

                if (!blockedModules.IsBlocked(x, x))
                    qrCode.ModuleMatrix[x + 4][x + 4] ^= selectedPatternFunc(x, x);
            }

            return selectedPattern;
        }

        /// <summary>Writes data bits into the module matrix, skipping blocked modules.</summary>
        /// <param name="qrCode">QR matrix that receives the data bits.</param>
        /// <param name="data">Data bits to place.</param>
        /// <param name="blockedModules">
        /// Rectangles that must stay unchanged (format, version, and other reserved areas).
        /// </param>
        public static void PlaceDataWords(QRIsoMatrix qrCode, BitArray data, BlockedModules blockedModules)
        {
            var size = qrCode.ModuleMatrix.Count - 8; // matrix size without quiet zone
            var up = true; // fill direction: up then down
            var index = 0; // next bit in data
            var count = data.Length; // bits to place

            // Rightmost column toward the left, two columns at a time.
            for (var x = size - 1; x >= 0; x -= 2) {
                // Skip timing column 6 (standard QR only, not Micro QR).
                if (qrCode.Version > 0 && x == 6)
                    x = 5;

                // Rows in this column pair.
                for (var yMod = 1; yMod <= size; yMod++) {
                    // y from the current fill direction.
                    var y = up ? size - yMod : yMod - 1;

                    // Write a bit when there is data left and the module is free.
                    if (index < count && !blockedModules.IsBlocked(x, y))
                        qrCode.ModuleMatrix[y + 4][x + 4] = data[index++];

                    if (index < count && x > 0 && !blockedModules.IsBlocked(x - 1, y))
                        qrCode.ModuleMatrix[y + 4][x - 1 + 4] = data[index++];
                }

                // Flip direction after each column pair.
                up = !up;
            }
        }

        /// <summary>Blocks separator areas around finder patterns so data placement cannot overwrite them.</summary>
        /// <param name="version">QR version (how many finder patterns).</param>
        /// <param name="size">QR matrix size.</param>
        /// <param name="blockedModules">Rectangles that must not be overwritten.</param>
        public static void ReserveSeperatorAreas(int version, int size, BlockedModules blockedModules)
        {
            // Top-left finder separators
            blockedModules.Add(new(7, 0, 1, 8)); // vertical strip by top-left finder
            blockedModules.Add(new(0, 7, 7, 1)); // horizontal strip by top-left finder
            if (version > 0) // standard QR has 3 finders
            {
                // Bottom-left finder separators
                blockedModules.Add(new(0, size - 8, 8, 1)); // horizontal strip by bottom-left finder
                blockedModules.Add(new(7, size - 7, 1, 7)); // vertical strip by bottom-left finder
                // Top-right finder separators
                blockedModules.Add(new(size - 8, 0, 1, 8)); // vertical strip by top-right finder
                blockedModules.Add(new(size - 7, 7, 7, 1)); // horizontal strip by top-right finder
            }
        }

        /// <summary>Blocks version-info areas for versions 7+ and format-info areas.</summary>
        /// <param name="size">QR matrix size.</param>
        /// <param name="version">QR version (where version info is placed).</param>
        /// <param name="blockedModules">Rectangles that must not be overwritten.</param>
        public static void ReserveVersionAreas(int size, int version, BlockedModules blockedModules)
        {
            if (version < 0) // Micro QR
            {
                blockedModules.Add(new(0, 8, 9, 1));
                blockedModules.Add(new(8, 0, 1, 8));
                return;
            }

            // Version and format areas beside the timing patterns.
            blockedModules.Add(new(8, 0, 1, 6)); // beside top timing
            blockedModules.Add(new(8, 7, 1, 1)); // small square by top-left finder
            blockedModules.Add(new(0, 8, 6, 1)); // beside left timing
            blockedModules.Add(new(7, 8, 2, 1)); // extension of the left-timing block
            blockedModules.Add(new(size - 8, 8, 8, 1)); // beside right timing
            blockedModules.Add(new(8, size - 7, 1, 7)); // beside bottom timing

            // Extra version-info blocks from version 7 up.
            if (version >= 7) {
                blockedModules.Add(new(size - 11, 0, 3, 6)); // top-right version info
                blockedModules.Add(new(0, size - 11, 6, 3)); // bottom-left version info
            }
        }

        /// <summary>Writes the required dark module at the spec position for standard QR codes.</summary>
        /// <param name="qrCode">QR matrix that receives the dark module.</param>
        /// <param name="version">QR version (dark-module location).</param>
        /// <param name="blockedModules">Rectangles that must not be overwritten; updated to include the dark module.</param>
        public static void PlaceDarkModule(QRIsoMatrix qrCode, int version, BlockedModules blockedModules)
        {
            // Micro QR has no dark module
            if (version < 0)
                return;

            // Dark module is always black.
            qrCode.ModuleMatrix[4 * version + 9 + 4][8 + 4] = true;
            // Block it so later steps cannot overwrite it.
            blockedModules.Add(new(8, 4 * version + 9, 1, 1));
        }

        /// <summary>Writes finder patterns so scanners can orient and recognize the symbol.</summary>
        /// <param name="qrCode">QR matrix that receives the finder patterns.</param>
        /// <param name="blockedModules">Rectangles that must not be overwritten; updated with finder areas.</param>
        public static void PlaceFinderPatterns(QRIsoMatrix qrCode, BlockedModules blockedModules)
        {
            var size = qrCode.ModuleMatrix.Count - 8;

            // Three finders: top-left, top-right, bottom-left.
            var count = qrCode.Version < 0 ? 1 : 3; // Micro QR has one finder.
            for (var i = 0; i < count; i++) {
                // Origin of this finder from i.
                var locationX = i == 1 ? size - 7 : 0; // i == 1 → top-right; else left (top or bottom)
                var locationY = i == 2 ? size - 7 : 0; // i == 2 → bottom-left; else top (left or right)

                // Draw the 7x7 finder at that origin.
                for (var x = 0; x < 7; x++) {
                    for (var y = 0; y < 7; y++) {
                        // 7x7 black ring, white ring, filled 3x3 center.
                        if (!(((x == 1 || x == 5) && y > 0 && y < 6) || (x > 0 && x < 6 && (y == 1 || y == 5))))
                            qrCode.ModuleMatrix[y + locationY + 4][x + locationX + 4] = true;
                    }
                }

                // Block this finder so data is not written over it.
                blockedModules.Add(new(locationX, locationY, 7, 7));
            }
        }

        /// <summary>Writes alignment patterns so scanners can read the symbol at different scales and angles.</summary>
        /// <param name="qrCode">QR matrix that receives the alignment patterns.</param>
        /// <param name="alignmentPatternLocations">Centers of the alignment patterns.</param>
        /// <param name="blockedModules">Rectangles that must not be overwritten; updated with alignment areas.</param>
        public static void PlaceAlignmentPatterns(QRIsoMatrix qrCode, List<Point> alignmentPatternLocations, BlockedModules blockedModules)
        {
            // Each requested alignment center.
            foreach (var loc in alignmentPatternLocations) {
                // 5x5 rectangle around the center.
                var alignmentPatternRect = new Rectangle(loc.X, loc.Y, 5, 5);

                // Skip if this rectangle overlaps a blocked area.
                if (blockedModules.IsBlocked(alignmentPatternRect)) {
                    // Avoid overwriting reserved modules.
                    continue;
                }

                // Fill the 5x5 alignment pattern.
                // 3x3 center with a one-module border.
                for (var x = 0; x < 5; x++) {
                    for (var y = 0; y < 5; y++) {
                        // Outer ring plus the center module.
                        if (y == 0 || y == 4 || x == 0 || x == 4 || (x == 2 && y == 2))
                            qrCode.ModuleMatrix[loc.Y + y + 4][loc.X + x + 4] = true;
                    }
                }

                // Block this alignment so later steps cannot overwrite it.
                blockedModules.Add(new(loc.X, loc.Y, 5, 5));
            }
        }

        /// <summary>Writes alternating timing patterns so scanners can map module coordinates.</summary>
        /// <param name="qrCode">QR matrix that receives the timing patterns.</param>
        /// <param name="blockedModules">Rectangles that must not be overwritten; updated with timing areas.</param>
        public static void PlaceTimingPatterns(QRIsoMatrix qrCode, BlockedModules blockedModules)
        {
            // Matrix size without padding.
            var size = qrCode.ModuleMatrix.Count - 8;
            if (qrCode.Version > 0) {
                // Timing from module 8 to size-8 so finders are not covered.
                for (var i = 8; i < size - 8; i++) {
                    if (i % 2 == 0) // dark every other module
                    {
                        qrCode.ModuleMatrix[6 + 4][i + 4] = true; // horizontal timing
                        qrCode.ModuleMatrix[i + 4][6 + 4] = true; // vertical timing
                    }
                }

                // Block the timing strips.
                blockedModules.Add(new(6, 8, 1, size - 16)); // horizontal timing
                blockedModules.Add(new(8, 6, size - 16, 1)); // vertical timing
            }
            else // Micro QR
            {
                // Timing from module 8 so the finder is not covered.
                for (var i = 8; i < size; i++) {
                    if (i % 2 == 0) // dark every other module
                    {
                        qrCode.ModuleMatrix[4][i + 4] = true; // horizontal timing
                        qrCode.ModuleMatrix[i + 4][4] = true; // vertical timing
                    }
                }

                // Block the timing strips.
                blockedModules.Add(new(0, 8, 1, size - 8)); // horizontal timing
                blockedModules.Add(new(8, 0, size - 8, 1)); // vertical timing
            }
        }
    }
}