namespace Lyo.Common.Pathing;

/// <summary>
/// Selects path separator and normalization rules for <see cref="PathHelpers" />.
/// Use <see cref="Host" /> for real OS filesystems; use <see cref="Posix" /> for virtual or remote roots (in-memory, SFTP) that always use <c>/</c>.
/// </summary>
public enum PathStyle
{
    /// <summary>Delegate to <see cref="System.IO.Path" /> (OS directory separator, drive letters on Windows).</summary>
    Host = 0,

    /// <summary>Always use <c>/</c>; no drive letters. Suitable for in-memory and remote (e.g. SFTP) path spaces.</summary>
    Posix = 1
}
