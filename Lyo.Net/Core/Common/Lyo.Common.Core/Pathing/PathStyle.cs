namespace Lyo.Common.Core.Pathing;

/// <summary>
/// Path-separator and normalization rules for <see cref="PathHelpers" />. Choose <see cref="Host" /> for a real OS filesystem; choose <see cref="Posix" /> for virtual or
/// remote roots (in-memory, SFTP) that always use <c>/</c>.
/// </summary>
public enum PathStyle
{
    /// <summary>Follow <see cref="System.IO.Path" /> (OS directory separator, Windows drive letters).</summary>
    Host = 0,

    /// <summary>Always <c>/</c>, no drive letters. Fits in-memory and remote (for example SFTP) path spaces.</summary>
    Posix = 1
}