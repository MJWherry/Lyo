using Lyo.Exceptions;

namespace Lyo.Hashing;

internal static class HashStreamValidation
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