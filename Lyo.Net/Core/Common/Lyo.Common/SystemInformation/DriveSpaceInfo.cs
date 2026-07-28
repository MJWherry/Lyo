namespace Lyo.Common.SystemInformation;

/// <summary>Size and free-space details for a single logical drive.</summary>
/// <param name="Name">Drive name (e.g. <c>C:\</c> or <c>/</c>).</param>
/// <param name="Type">Drive type (e.g. <c>Fixed</c>, <c>Network</c>, <c>Removable</c>).</param>
/// <param name="Format">File system format (e.g. <c>NTFS</c>, <c>ext4</c>).</param>
/// <param name="TotalSizeBytes">Total size of the drive in bytes.</param>
/// <param name="AvailableFreeSpaceBytes">Free space available to the current user in bytes.</param>
public sealed record DriveSpaceInfo(string Name, string Type, string Format, long TotalSizeBytes, long AvailableFreeSpaceBytes);