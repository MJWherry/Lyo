using System.Runtime.CompilerServices;

namespace Lyo.Common.Core.Extensions;

/// <summary>Helpers on <see cref="Stream" />.</summary>
public static class StreamExtensions
{
    /// <summary>Rewinds a seekable stream to position 0 when it is not already there.</summary>
    /// <param name="source">Stream to rewind.</param>
    /// <param name="throwOnUnSeekable">When <see langword="true" />, a non-seekable stream past position 0 throws <see cref="InvalidOperationException" />.</param>
    /// <exception cref="InvalidOperationException">The stream cannot seek, position is greater than zero, and <paramref name="throwOnUnSeekable" /> is <see langword="true" />.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MoveToStart(this Stream source, bool throwOnUnSeekable = false)
        => source.Position = source switch {
            { CanSeek: true, Position: > 0 } => 0,
            { CanSeek: false, Position: > 0 } when throwOnUnSeekable => throw new InvalidOperationException(""),
            var _ => source.Position
        };
}