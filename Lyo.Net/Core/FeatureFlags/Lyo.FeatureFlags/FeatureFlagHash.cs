using System.Text;

namespace Lyo.FeatureFlags;

/// <summary>Stable 0–99 bucket for a flag name and stickiness key. This is not the runtime string hash.</summary>
internal static class FeatureFlagHash
{
    /// <summary>FNV-1a 32-bit over a little-endian length prefix and the UTF-8 bytes of <paramref name="flagName" /> and <paramref name="stickinessKey" />.</summary>
    /// <remarks><c>billing</c> and <c>user-a</c> return 50. The hash stays in a <see cref="uint" /> so the bucket is not a signed remainder.</remarks>
    internal static int Bucket(string flagName, string stickinessKey)
    {
        var hash = 2166136261u;
        Mix(ref hash, Encoding.UTF8.GetBytes(flagName));
        Mix(ref hash, Encoding.UTF8.GetBytes(stickinessKey));
        return (int)(hash % 100u);
    }

    private static void Mix(ref uint hash, byte[] bytes)
    {
        MixByte(ref hash, (byte)bytes.Length);
        MixByte(ref hash, (byte)(bytes.Length >> 8));
        MixByte(ref hash, (byte)(bytes.Length >> 16));
        MixByte(ref hash, (byte)(bytes.Length >> 24));
        foreach (var value in bytes)
            MixByte(ref hash, value);
    }

    private static void MixByte(ref uint hash, byte value)
    {
        hash ^= value;
        hash *= 16777619u;
    }
}