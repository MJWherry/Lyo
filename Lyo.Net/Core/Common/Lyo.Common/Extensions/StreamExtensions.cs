using System.Runtime.CompilerServices;

namespace Lyo.Common.Extensions;

/// <summary>Extension methods for <see cref="Stream" />.</summary>
public static class StreamExtensions
{
    /// <summary>Sets the stream position to the beginning when the stream is seekable and not already at the start.</summary>
    /// <param name="source">The stream to rewind.</param>
    /// <param name="throwOnUnSeekable">If <see langword="true" /> and the stream is not seekable but its position is past zero, throws <see cref="InvalidOperationException" />.</param>
    /// <exception cref="InvalidOperationException">The stream cannot seek, its position is greater than zero, and <paramref name="throwOnUnSeekable" /> is <see langword="true" />.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MoveToStart(this Stream source, bool throwOnUnSeekable = false)
        => source.Position = source switch {
            { CanSeek: true, Position: > 0 } => 0,
            { CanSeek: false, Position: > 0 } when throwOnUnSeekable => throw new InvalidOperationException(""),
            var _ => source.Position
        };
}