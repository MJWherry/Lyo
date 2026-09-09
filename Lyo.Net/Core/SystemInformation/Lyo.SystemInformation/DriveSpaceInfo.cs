namespace Lyo.SystemInformation;

/// <summary>Size and free-space facts for one logical drive.</summary>
/// <param name="Name">Drive name (for example <c>C:\</c> or <c>/</c>).</param>
/// <param name="Type">Drive type (for example <c>Fixed</c>, <c>Network</c>, <c>Removable</c>).</param>
/// <param name="Format">File-system format (for example <c>NTFS</c>, <c>ext4</c>).</param>
/// <param name="TotalSizeBytes">Total capacity of the drive in bytes.</param>
/// <param name="AvailableFreeSpaceBytes">Free space available to the current user, in bytes.</param>
public sealed record DriveSpaceInfo(string Name, string Type, string Format, long TotalSizeBytes, long AvailableFreeSpaceBytes);