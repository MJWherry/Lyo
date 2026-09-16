namespace Lyo.Desktop;

internal static class WindowsVirtualKey
{
    internal static uint Code(string key)
    {
        if (key.Equals("Control", StringComparison.OrdinalIgnoreCase))
            return 0x11;
        if (key.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            return 0x10;
        if (key.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            return 0x12;
        if (key.Equals("Super", StringComparison.OrdinalIgnoreCase))
            return 0x5B;
        if (key.Length == 1) {
            var c = char.ToUpperInvariant(key[0]);
            if (c is >= 'A' and <= 'Z' or >= '0' and <= '9')
                return c;
        }

        if (key.Equals("Escape", StringComparison.OrdinalIgnoreCase))
            return 0x1B;
        if (key.Equals("Space", StringComparison.OrdinalIgnoreCase))
            return 0x20;
        if (key.Equals("Tab", StringComparison.OrdinalIgnoreCase))
            return 0x09;
        if (key.StartsWith('F') && int.TryParse(key.AsSpan(1), out var n) && n is >= 1 and <= 24)
            return (uint)(0x6F + n);
        throw new InvalidOperationException($"Unknown key '{key}'.");
    }
}
