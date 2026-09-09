namespace Lyo.Privacy.Internal;

internal static class SpanRedaction
{
    /// <summary>UTF-16 <see cref="string" /> built from a span; modern targets use <c>new string(span)</c>.</summary>
    public static string ToString(ReadOnlySpan<char> input)
    {
#if NETSTANDARD2_0
        if (input.Length == 0)
            return string.Empty;

        var arr = new char[input.Length];
        input.CopyTo(arr);
        return new(arr);
#else
        return new(input);
#endif
    }
}