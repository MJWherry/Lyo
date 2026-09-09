using Lyo.Exceptions;

namespace Lyo.Streams;

/// <summary>Shared buffer-range checks used by stream read/write helpers.</summary>
internal static class StreamValidation
{
    public static void ValidateReadBuffer(byte[]? buffer, int offset, int count)
    {
        ArgumentHelpers.ThrowIfNull(buffer);
        ArgumentHelpers.ThrowIfNotInRange(offset, 0, buffer.Length);
        ArgumentHelpers.ThrowIfNotInRange(count, 0, buffer.Length - offset);
    }

    public static void ValidateWriteBuffer(byte[]? buffer, int offset, int count)
    {
        ArgumentHelpers.ThrowIfNull(buffer);
        ArgumentHelpers.ThrowIfNotInRange(offset, 0, buffer.Length);
        ArgumentHelpers.ThrowIfNotInRange(count, 0, buffer.Length - offset);
    }
}