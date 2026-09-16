namespace Lyo.Nfc;

/// <summary>
/// Page map for an NTAG21x chip. User memory starts at page 4. Config and lock pages are outside <see cref="UserPageCount"/> and are not written by <see cref="NfcNdef"/>.
/// </summary>
public sealed class NtagLayout
{
    internal NtagLayout(string name, int totalPages, int userPageCount)
    {
        Name = name;
        TotalPages = totalPages;
        UserPageCount = userPageCount;
    }

    /// <summary>NTAG213, NTAG215, or NTAG216.</summary>
    public string Name { get; }

    /// <summary>Pages on the chip, including UID, lock, and config pages.</summary>
    public int TotalPages { get; }

    /// <summary>First user-memory page. Always 4 on NTAG21x.</summary>
    public int UserStartPage => 4;

    /// <summary>User pages that may hold an NDEF TLV.</summary>
    public int UserPageCount { get; }

    /// <summary>User memory size in bytes (<see cref="UserPageCount"/> × 4).</summary>
    public int UserCapacityBytes => UserPageCount * 4;
}

/// <summary>Known NTAG21x layouts and the capability-container byte that selects one.</summary>
public static class NtagMemory
{
    /// <summary>144 bytes of user memory, pages 4 through 39.</summary>
    public static NtagLayout Ntag213 { get; } = new("NTAG213", totalPages: 45, userPageCount: 36);

    /// <summary>504 bytes of user memory, pages 4 through 129.</summary>
    public static NtagLayout Ntag215 { get; } = new("NTAG215", totalPages: 135, userPageCount: 126);

    /// <summary>888 bytes of user memory, pages 4 through 225.</summary>
    public static NtagLayout Ntag216 { get; } = new("NTAG216", totalPages: 231, userPageCount: 222);

    /// <summary>
    /// Picks a layout from a Type 2 capability container (page 3). Unknown or short pages fall back to <see cref="Ntag213"/>.
    /// </summary>
    /// <param name="capabilityContainer">At least the first three bytes of page 3 (<c>E1 10 xx</c>).</param>
    public static NtagLayout FromCapabilityContainer(ReadOnlySpan<byte> capabilityContainer)
    {
        if (capabilityContainer.Length < 3 || capabilityContainer[0] != 0xE1 || capabilityContainer[1] != 0x10)
            return Ntag213;

        return capabilityContainer[2] switch {
            0x12 => Ntag213,
            0x3E => Ntag215,
            0x6D => Ntag216,
            _ => Ntag213
        };
    }
}
