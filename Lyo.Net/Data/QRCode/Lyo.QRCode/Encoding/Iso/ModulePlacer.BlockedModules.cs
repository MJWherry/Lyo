namespace Lyo.QRCode.Encoding.Iso;

internal sealed partial class QRIsoEncoder
{
    private static partial class ModulePlacer
    {
        /// <summary>Blocked modules represented as rectangles.</summary>
        public struct BlockedModules : IDisposable
        {
            private readonly BitArray[] _blockedModules;

            private static BitArray[]? _staticBlockedModules;

            /// <summary>Builds a <see cref="BlockedModules" /> of the given size.</summary>
            /// <param name="size">Size of the blocked-modules matrix.</param>
            public BlockedModules(int size)
            {
                _blockedModules = Interlocked.Exchange(ref _staticBlockedModules, null)!;
                if (_blockedModules != null && _blockedModules.Length >= size) {
                    for (var i = 0; i < size; i++)
                        _blockedModules[i].SetAll(false);
                }
                else {
                    _blockedModules = new BitArray[size];
                    for (var i = 0; i < size; i++)
                        _blockedModules[i] = new(size);
                }
            }

            /// <summary>Marks a module blocked at the given coordinates.</summary>
            /// <param name="x">X-coordinate of the module.</param>
            /// <param name="y">Y-coordinate of the module.</param>
            public void Add(int x, int y) => _blockedModules[y][x] = true;

            /// <summary>Marks the modules inside <paramref name="rect" /> as blocked.</summary>
            /// <param name="rect">Rectangle that defines the blocked region.</param>
            public void Add(Rectangle rect)
            {
                for (var y = rect.Y; y < rect.Y + rect.Height; y++) {
                    for (var x = rect.X; x < rect.X + rect.Width; x++)
                        _blockedModules[y][x] = true;
                }
            }

            /// <summary>Whether the given coordinates are blocked.</summary>
            /// <param name="x">X-coordinate to test.</param>
            /// <param name="y">Y-coordinate to test.</param>
            /// <returns><c>true</c> if the coordinates are blocked; otherwise <c>false</c>.</returns>
            public bool IsBlocked(int x, int y) => _blockedModules[y][x];

            /// <summary>Whether <paramref name="r1" /> is blocked.</summary>
            /// <param name="r1">Rectangle to test.</param>
            /// <returns><c>true</c> if the rectangle is blocked; otherwise <c>false</c>.</returns>
            public bool IsBlocked(Rectangle r1)
            {
                for (var y = r1.Y; y < r1.Y + r1.Height; y++) {
                    for (var x = r1.X; x < r1.X + r1.Width; x++) {
                        if (_blockedModules[y][x])
                            return true;
                    }
                }

                return false;
            }

            public void Dispose() => Interlocked.CompareExchange(ref _staticBlockedModules, _blockedModules, null);
        }
    }
}