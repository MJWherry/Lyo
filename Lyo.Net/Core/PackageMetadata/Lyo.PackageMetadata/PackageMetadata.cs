namespace Lyo.PackageMetadata;

/// <summary>NuGet-gallery-style metadata for a package, suitable for JSON and database persistence. Multiple rows may share the same <see cref="Name" />
/// with different <see cref="Version" /> values to represent published package versions.</summary>
/// <param name="Id">Primary key when this row is stored (e.g. Postgres <c>uuid</c>).</param>
/// <param name="Name">NuGet package id / name (e.g. <c>Npgsql</c>).</param>
/// <param name="Version">NuGet package version string for this row (e.g. SemVer <c>8.0.0</c>, as returned by the feed). Optional when unknown.</param>
/// <param name="PackageFileSha512Hex">SHA-512 of the package archive bytes (typically <c>.nupkg</c>) as 128 lowercase hex characters, aligned with NuGet registration SHA512; use <see cref="PackageFileSha512.ComputeHex(byte[])" />.</param>
/// <param name="CreatedAt">When this metadata row was first stored (UTC). Populated by Postgres store; optional elsewhere.</param>
/// <param name="UpdatedAt">When this metadata row was last updated (UTC). Populated by Postgres store; optional elsewhere.</param>
public sealed record PackageMetadata(
    Guid Id,
    string Name,
    string? Version = null,
    string? PackageFileSha512Hex = null,
    string? Title = null,
    string? Description = null,
    IReadOnlyList<string>? Authors = null,
    IReadOnlyList<string>? PackageTypes = null,
    string? ProjectUrl = null,
    string? RepositoryUrl = null,
    string? LicenseUrl = null,
    string? LicenseExpression = null,
    string? PackageDetailsUrl = null,
    IReadOnlyList<string>? Tags = null,
    DateTime? CreatedAt = null,
    DateTime? UpdatedAt = null);
