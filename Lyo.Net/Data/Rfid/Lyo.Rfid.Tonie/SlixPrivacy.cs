using Lyo.Exceptions;

namespace Lyo.Rfid.Tonie;

/// <summary>
/// Session password for ICODE SLIX-L. The value on the air is <c>Password XOR {Random, Random}</c>, where <c>Random</c> is the two-byte number from the last GET RANDOM
/// NUMBER.
/// </summary>
public static class SlixPrivacy
{
    /// <summary>Derives the 4-byte XOR password. <paramref name="password" /> is 4 bytes and <paramref name="random" /> is 2 bytes.</summary>
    public static byte[] XorPassword(ReadOnlySpan<byte> password, ReadOnlySpan<byte> random)
    {
        ArgumentHelpers.ThrowIfNotEqual(password.Length, 4);
        ArgumentHelpers.ThrowIfNotEqual(random.Length, 2);
        return [(byte)(password[0] ^ random[0]), (byte)(password[1] ^ random[1]), (byte)(password[2] ^ random[0]), (byte)(password[3] ^ random[1])];
    }
}