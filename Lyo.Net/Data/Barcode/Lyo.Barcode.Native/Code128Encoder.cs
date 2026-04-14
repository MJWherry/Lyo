namespace Lyo.Barcode.Native;

/// <summary>Encodes Code 128 subset B (ASCII 32–127) to a row of modules (false = white, true = black).</summary>
internal static class Code128Encoder
{
    /// <exception cref="ArgumentException">Empty or invalid characters for Code 128 B.</exception>
    internal static bool[] EncodeCode128B(string contents)
    {
        ArgumentException Throw(string m) => new(m, nameof(contents));

        if (string.IsNullOrEmpty(contents))
            throw Throw("Barcode data cannot be empty.");

        var patterns = new List<int[]>();

        void AddPattern(int patternIndex) => patterns.Add(Code128Patterns.Patterns[patternIndex]);

        AddPattern(Code128Patterns.CodeStartB);

        // ISO/IEC 15417 / GS1: checksum = (CodeStartB + Σ dataCode_i × weight_i) mod 103 with weight 1..n for n data characters.
        var sum = Code128Patterns.CodeStartB;
        for (var i = 0; i < contents.Length; i++) {
            var c = contents[i];
            if (c is < (char)32 or > (char)127)
                throw Throw($"Character U+{(int)c:X4} cannot be encoded in Code 128 B (use ASCII 32–127).");

            var value = c - ' ';
            sum += value * (i + 1);
            AddPattern(value);
        }

        var checkDigit = PosMod(sum, 103);
        patterns.Add(Code128Patterns.Patterns[checkDigit]);
        patterns.Add(Code128Patterns.Patterns[Code128Patterns.CodeStop]);
        var totalWidth = 0;
        foreach (var p in patterns) {
            foreach (var w in p)
                totalWidth += w;
        }

        var row = new bool[totalWidth];
        var pos = 0;
        foreach (var pattern in patterns)
            pos += AppendPattern(row, pos, pattern, true);

        return row;
    }

    /// <summary>Writes alternating black/white runs; first run is black when <paramref name="startBlack" /> is true.</summary>
    internal static int AppendPattern(bool[] target, int offset, int[] pattern, bool startBlack)
    {
        var color = startBlack;
        var added = 0;
        foreach (var len in pattern) {
            for (var j = 0; j < len; j++)
                target[offset + added++] = color;

            color = !color;
        }

        return added;
    }

    private static int PosMod(int value, int m) => (value % m + m) % m;
}