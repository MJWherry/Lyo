namespace Lyo.QRCode.Encoding.Iso;

internal sealed partial class QRIsoEncoder
{
    private static partial class ModulePlacer
    {
        /// <summary>
        /// Mask-pattern helpers for QR generation. Patterns break up regular runs in the data matrix that can confuse scanners.
        /// </summary>
        private static class MaskPattern
        {
            /// <summary>Mask-pattern index mapped to a function that decides whether a module is masked.</summary>
            public static readonly List<Func<int, int, bool>> Patterns = new(8) {
                Pattern1,
                Pattern2,
                Pattern3,
                Pattern4,
                Pattern5,
                Pattern6,
                Pattern7,
                Pattern8
            };

            /// <summary>Mask pattern 1: (x + y) % 2 == 0. Checkerboard.</summary>
            public static bool Pattern1(int x, int y) => (x + y) % 2 == 0;

            /// <summary>Mask pattern 2: y % 2 == 0. Horizontal stripes.</summary>
            public static bool Pattern2(int x, int y) => y % 2 == 0;

            /// <summary>Mask pattern 3: x % 3 == 0. Vertical stripes.</summary>
            public static bool Pattern3(int x, int y) => x % 3 == 0;

            /// <summary>Mask pattern 4: (x + y) % 3 == 0. Diagonal stripes.</summary>
            public static bool Pattern4(int x, int y) => (x + y) % 3 == 0;

            /// <summary>Mask pattern 5: ((y / 2) + (x / 3)) % 2 == 0. Mix of horizontal and vertical rules.</summary>
            public static bool Pattern5(int x, int y) => (int)(Math.Floor(y / 2d) + Math.Floor(x / 3d)) % 2 == 0;

            /// <summary>Mask pattern 6: ((x * y) % 2 + (x * y) % 3) == 0. Product of x and y, modulo 2 and 3.</summary>
            public static bool Pattern6(int x, int y) => x * y % 2 + x * y % 3 == 0;

            /// <summary>Mask pattern 7: (((x * y) % 2 + (x * y) % 3) % 2) == 0. Product of x and y, then modulo 2.</summary>
            public static bool Pattern7(int x, int y) => (x * y % 2 + x * y % 3) % 2 == 0;

            /// <summary>Mask pattern 8: (((x + y) % 2) + ((x * y) % 3) % 2) == 0. Checker plus multiplicative mask.</summary>
            public static bool Pattern8(int x, int y) => ((x + y) % 2 + x * y % 3) % 2 == 0;

            /// <summary>
            /// Penalty score for a Micro QR mask. Lower scores are easier for decoders to read accurately.
            /// </summary>
            /// <param name="qrCode">QR data to score.</param>
            /// <returns>Total penalty score.</returns>
            public static int ScoreMicro(QRIsoMatrix qrCode)
            {
                var size = qrCode.ModuleMatrix.Count;
                var sum1 = 0;
                var sum2 = 0;
                for (var i = 1; i < size; i++) {
                    if (qrCode.ModuleMatrix[size - 1][i])
                        sum1++;

                    if (qrCode.ModuleMatrix[i][size - 1])
                        sum2++;
                }

                var total = sum1 < sum2 ? sum1 * 16 + sum2 : sum2 * 16 + sum1;
                return -total; // negate so lower is better
            }

            /// <summary>
            /// Penalty score for a QR mask. Lower scores are easier for decoders to read accurately. Sum of the four penalty rules.
            /// </summary>
            /// <param name="qrCode">QR data to score.</param>
            /// <returns>Total penalty score.</returns>
            public static int Score(QRIsoMatrix qrCode)
            {
                int score1 = 0, // five or more same-color modules in a row or column
                    score2 = 0, // same-color 2x2 blocks
                    score3 = 0, // finder-like patterns
                    score4 = 0; // dark/light module ratio away from 50%

                var size = qrCode.ModuleMatrix.Count;

                // Penalty 1: consecutive same-color modules in rows and columns
                for (var y = 0; y < size; y++) {
                    var modInRow = 0;
                    var modInColumn = 0;
                    var lastValRow = qrCode.ModuleMatrix[y][0];
                    var lastValColumn = qrCode.ModuleMatrix[0][y];
                    for (var x = 0; x < size; x++) {
                        // Consecutive modules in the row
                        if (qrCode.ModuleMatrix[y][x] == lastValRow)
                            modInRow++;
                        else
                            modInRow = 1;

                        if (modInRow == 5)
                            score1 += 3;
                        else if (modInRow > 5)
                            score1++;

                        lastValRow = qrCode.ModuleMatrix[y][x];

                        // Consecutive modules in the column
                        if (qrCode.ModuleMatrix[x][y] == lastValColumn)
                            modInColumn++;
                        else
                            modInColumn = 1;

                        if (modInColumn == 5)
                            score1 += 3;
                        else if (modInColumn > 5)
                            score1++;

                        lastValColumn = qrCode.ModuleMatrix[x][y];
                    }
                }

                // Penalty 2: same-color 2x2 blocks
                for (var y = 0; y < size - 1; y++) {
                    for (var x = 0; x < size - 1; x++) {
                        if (qrCode.ModuleMatrix[y][x] == qrCode.ModuleMatrix[y][x + 1] && qrCode.ModuleMatrix[y][x] == qrCode.ModuleMatrix[y + 1][x] &&
                            qrCode.ModuleMatrix[y][x] == qrCode.ModuleMatrix[y + 1][x + 1])
                            score2 += 3;
                    }
                }

                // Penalty 3: finder-like patterns to avoid
                for (var y = 0; y < size; y++) {
                    for (var x = 0; x < size - 10; x++) {
                        // Horizontal match
                        if ((qrCode.ModuleMatrix[y][x] && !qrCode.ModuleMatrix[y][x + 1] && qrCode.ModuleMatrix[y][x + 2] && qrCode.ModuleMatrix[y][x + 3] &&
                                qrCode.ModuleMatrix[y][x + 4] && !qrCode.ModuleMatrix[y][x + 5] && qrCode.ModuleMatrix[y][x + 6] && !qrCode.ModuleMatrix[y][x + 7] &&
                                !qrCode.ModuleMatrix[y][x + 8] && !qrCode.ModuleMatrix[y][x + 9] && !qrCode.ModuleMatrix[y][x + 10]) || (!qrCode.ModuleMatrix[y][x] &&
                                !qrCode.ModuleMatrix[y][x + 1] && !qrCode.ModuleMatrix[y][x + 2] && !qrCode.ModuleMatrix[y][x + 3] && qrCode.ModuleMatrix[y][x + 4] &&
                                !qrCode.ModuleMatrix[y][x + 5] && qrCode.ModuleMatrix[y][x + 6] && qrCode.ModuleMatrix[y][x + 7] && qrCode.ModuleMatrix[y][x + 8] &&
                                !qrCode.ModuleMatrix[y][x + 9] && qrCode.ModuleMatrix[y][x + 10]))
                            score3 += 40;

                        // Vertical match
                        if ((qrCode.ModuleMatrix[x][y] && !qrCode.ModuleMatrix[x + 1][y] && qrCode.ModuleMatrix[x + 2][y] && qrCode.ModuleMatrix[x + 3][y] &&
                                qrCode.ModuleMatrix[x + 4][y] && !qrCode.ModuleMatrix[x + 5][y] && qrCode.ModuleMatrix[x + 6][y] && !qrCode.ModuleMatrix[x + 7][y] &&
                                !qrCode.ModuleMatrix[x + 8][y] && !qrCode.ModuleMatrix[x + 9][y] && !qrCode.ModuleMatrix[x + 10][y]) || (!qrCode.ModuleMatrix[x][y] &&
                                !qrCode.ModuleMatrix[x + 1][y] && !qrCode.ModuleMatrix[x + 2][y] && !qrCode.ModuleMatrix[x + 3][y] && qrCode.ModuleMatrix[x + 4][y] &&
                                !qrCode.ModuleMatrix[x + 5][y] && qrCode.ModuleMatrix[x + 6][y] && qrCode.ModuleMatrix[x + 7][y] && qrCode.ModuleMatrix[x + 8][y] &&
                                !qrCode.ModuleMatrix[x + 9][y] && qrCode.ModuleMatrix[x + 10][y]))
                            score3 += 40;
                    }
                }

                // Penalty 4: dark vs light module ratio
                var blackModules = 0;
                foreach (var bitArray in qrCode.ModuleMatrix) {
                    for (var x = 0; x < size; x++) {
                        if (bitArray[x])
                            blackModules++;
                    }
                }

                var percentDiv5 = blackModules * 20 / (qrCode.ModuleMatrix.Count * qrCode.ModuleMatrix.Count);
                var prevMultipleOf5 = Math.Abs(percentDiv5 - 10);
                var nextMultipleOf5 = Math.Abs(percentDiv5 - 9);
                score4 = Math.Min(prevMultipleOf5, nextMultipleOf5) * 10;

                // Sum of the four penalties
                return score1 + score2 + score3 + score4;
            }
        }
    }
}