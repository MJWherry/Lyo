using System.Buffers;

namespace Lyo.Diff.Text;

/// <summary>Myers O(ND) diff with trace-based backtracking (see J. Coglan, "The Myers diff algorithm: part 3").</summary>
internal static class MyersDiffCalculator
{
    public static List<TextDiffChunk> Compute(string oldText, TextToken[] oldTokens, string newText, TextToken[] newTokens)
    {
        var n = oldTokens.Length;
        var m = newTokens.Length;
        if (n == 0 && m == 0)
            return [];

        if (n == 0)
            return InsertOnlyChunks(newTokens);

        if (m == 0)
            return DeleteOnlyChunks(oldTokens);

        var max = n + m;
        var len = 2 * max + 1;
        var v = ArrayPool<int>.Shared.Rent(len);
        try {
            for (var i = 0; i < len; i++)
                v[i] = -1;

            var offset = max;
            v[offset + 1] = 0;
            var trace = new List<int[]>();
            for (var d = 0; d <= max; d++) {
                trace.Add((int[])v.Clone());
                for (var k = -d; k <= d; k += 2) {
                    int x;
                    if (k == -d || (k != d && v[offset + k - 1] < v[offset + k + 1]))
                        x = v[offset + k + 1];
                    else
                        x = v[offset + k - 1] + 1;

                    var y = x - k;
                    while (x < n && y < m && TokenEquals(oldText, oldTokens[x], newText, newTokens[y])) {
                        x++;
                        y++;
                    }

                    v[offset + k] = x;
                    if (x >= n && y >= m)
                        return Backtrack(trace, d, n, m, offset, oldTokens, newTokens);
                }
            }

            throw new InvalidOperationException("Myers diff failed to terminate.");
        }
        finally {
            ArrayPool<int>.Shared.Return(v);
        }
    }

    private static List<TextDiffChunk> InsertOnlyChunks(TextToken[] newTokens)
    {
        var chunks = new List<TextDiffChunk>();
        var j = 0;
        while (j < newTokens.Length) {
            var nStart = newTokens[j].Start;
            var nLen = newTokens[j].Length;
            j++;
            while (j < newTokens.Length) {
                var t = newTokens[j];
                if (t.Start == nStart + nLen) {
                    nLen += t.Length;
                    j++;
                    continue;
                }

                break;
            }

            chunks.Add(new(TextDiffKind.Insert, 0, 0, nStart, nLen));
        }

        return chunks;
    }

    private static List<TextDiffChunk> DeleteOnlyChunks(TextToken[] oldTokens)
    {
        var chunks = new List<TextDiffChunk>();
        var i = 0;
        while (i < oldTokens.Length) {
            var oStart = oldTokens[i].Start;
            var oLen = oldTokens[i].Length;
            i++;
            while (i < oldTokens.Length) {
                var t = oldTokens[i];
                if (t.Start == oStart + oLen) {
                    oLen += t.Length;
                    i++;
                    continue;
                }

                break;
            }

            chunks.Add(new(TextDiffKind.Delete, oStart, oLen, 0, 0));
        }

        return chunks;
    }

    private static List<TextDiffChunk> Backtrack(List<int[]> trace, int dEnd, int n, int m, int offset, TextToken[] oldTokens, TextToken[] newTokens)
    {
        var x = n;
        var y = m;
        var ops = new List<EditOp>(n + m + 8);
        for (var d = dEnd; d >= 0; d--) {
            var v = trace[d];
            var k = x - y;
            int prevK;
            if (k == -d || (k != d && v[offset + k - 1] < v[offset + k + 1]))
                prevK = k + 1;
            else
                prevK = k - 1;

            var prevX = v[offset + prevK];
            var prevY = prevX - prevK;
            while (x > prevX && y > prevY) {
                x--;
                y--;
                ops.Add(new(TextDiffKind.Equal, x, y));
            }

            if (d <= 0)
                break;

            // Horizontal step in edit graph (delete old) vs vertical (insert new); see Myers / Coglan backtrack.
            if (x == prevX)
                ops.Add(new(TextDiffKind.Insert, prevX, prevY));
            else
                ops.Add(new(TextDiffKind.Delete, prevX, prevY));

            x = prevX;
            y = prevY;
        }

        ops.Reverse();
        return CoalesceToChunks(ops, oldTokens, newTokens);
    }

    private static List<TextDiffChunk> CoalesceToChunks(List<EditOp> ops, TextToken[] oldTokens, TextToken[] newTokens)
    {
        var chunks = new List<TextDiffChunk>();
        var i = 0;
        while (i < ops.Count) {
            var op = ops[i];
            if (op.Kind == TextDiffKind.Equal) {
                var oi = op.OldIndex;
                var ni = op.NewIndex;
                var oStart = oldTokens[oi].Start;
                var oLen = oldTokens[oi].Length;
                var nStart = newTokens[ni].Start;
                var nLen = newTokens[ni].Length;
                i++;
                while (i < ops.Count && ops[i].Kind == TextDiffKind.Equal) {
                    var e = ops[i];
                    var oTok = oldTokens[e.OldIndex];
                    var nTok = newTokens[e.NewIndex];
                    if (oTok.Start == oStart + oLen && nTok.Start == nStart + nLen) {
                        oLen += oTok.Length;
                        nLen += nTok.Length;
                        i++;
                        continue;
                    }

                    break;
                }

                chunks.Add(new(TextDiffKind.Equal, oStart, oLen, nStart, nLen));
                continue;
            }

            if (op.Kind == TextDiffKind.Delete) {
                var oi = op.OldIndex;
                var oStart = oldTokens[oi].Start;
                var oLen = oldTokens[oi].Length;
                i++;
                while (i < ops.Count && ops[i].Kind == TextDiffKind.Delete) {
                    var e = ops[i];
                    var oTok = oldTokens[e.OldIndex];
                    if (oTok.Start == oStart + oLen) {
                        oLen += oTok.Length;
                        i++;
                        continue;
                    }

                    break;
                }

                chunks.Add(new(TextDiffKind.Delete, oStart, oLen, 0, 0));
                continue;
            }

            {
                var ni = op.NewIndex;
                var nStart = newTokens[ni].Start;
                var nLen = newTokens[ni].Length;
                i++;
                while (i < ops.Count && ops[i].Kind == TextDiffKind.Insert) {
                    var e = ops[i];
                    var nTok = newTokens[e.NewIndex];
                    if (nTok.Start == nStart + nLen) {
                        nLen += nTok.Length;
                        i++;
                        continue;
                    }

                    break;
                }

                chunks.Add(new(TextDiffKind.Insert, 0, 0, nStart, nLen));
            }
        }

        return chunks;
    }

    private static bool TokenEquals(string oldText, TextToken o, string newText, TextToken n)
    {
        if (o.Length != n.Length)
            return false;

        return string.CompareOrdinal(oldText, o.Start, newText, n.Start, o.Length) == 0;
    }

    private readonly struct EditOp(TextDiffKind kind, int oldIndex, int newIndex)
    {
        public TextDiffKind Kind { get; } = kind;

        public int OldIndex { get; } = oldIndex;

        public int NewIndex { get; } = newIndex;
    }
}