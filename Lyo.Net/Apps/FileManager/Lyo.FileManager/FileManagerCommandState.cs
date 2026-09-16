using Lyo.Api.FileStorage.Models;
using Lyo.Exceptions;

namespace Lyo.FileManager;

/// <summary>Which File Manager commands the connected server allows.</summary>
public sealed record FileManagerCommandState(bool PresignedRead, bool Copy, bool Move, bool AccessLinks, bool Encryption)
{
    /// <summary>Maps storage capability flags onto the commands the window shows.</summary>
    public static FileManagerCommandState From(FileStorageCapabilitiesResponse capabilities)
    {
        ArgumentHelpers.ThrowIfNull(capabilities);
        return new(capabilities.PresignedRead, capabilities.Copy, capabilities.Move, capabilities.AccessLinks, capabilities.Encryption);
    }

    /// <summary>
    /// Encrypt stays off when the server has no key store. It also stays off when this server has no local key and <paramref name="keyIdCount" /> is zero.
    /// </summary>
    public bool CanEncrypt(bool hasKeyStore, int keyIdCount) => Encryption && (hasKeyStore || keyIdCount > 0);
}
