namespace Lyo.PackageMetadata;

/// <summary>Package catalog metadata for a published artifact, suitable for JSON and database persistence. Multiple rows may share the same
/// <see cref="Name" /> with different <see cref="Version" /> values. Interpretation of <see cref="Name" /> / <see cref="Version" /> depends on
/// <see cref="Ecosystem" /> (e.g. NuGet package id + version; Maven often uses <c>groupId:artifactId</c> as <see cref="Name" />).</summary>
/// <param name="Id">Primary key when this row is stored (e.g. Postgres <c>uuid</c>).</param>
/// <param name="Ecosystem">Registry or distribution family for this row.</param>
/// <param name="Name">Package identity (e.g. NuGet id, or Maven <c>groupId:artifactId</c>).</param>
/// <param name="Version">Published version string when known (ecosystem-specific format).</param>
/// <param name="ArtifactDigestAlgorithm">Hash algorithm for <see cref="ArtifactDigestHex" />; <see cref="ArtifactDigestAlgorithm.None" /> when no digest is stored.</param>
/// <param name="ArtifactDigestHex">Lowercase hex digest of canonical primary artifact bytes (length: 40 for SHA-1, 64 for SHA-256, 128 for SHA-512), or <see langword="null" />.</param>
/// <param name="LicenseExpression">When known, an SPDX 2.x license expression: <c>AND</c>, <c>OR</c>, parentheses, <c>WITH</c>. Canonical license field.</param>
/// <param name="LicenseExpressionSyntax">Parsed tree of <see cref="LicenseExpression" /> (surjective with the string when parsing succeeds). Derived when persisting or loading; not an independent legal source—use <see cref="LicenseExpression" /> as written.</param>
/// <param name="CreatedAt">When this metadata row was first stored (UTC). Populated by Postgres store; optional elsewhere.</param>
/// <param name="UpdatedAt">When this metadata row was last updated (UTC). Populated by Postgres store; optional elsewhere.</param>
public sealed record PackageMetadata(
    Guid Id,
    PackageEcosystem Ecosystem,
    string Name,
    string? Version = null,
    ArtifactDigestAlgorithm ArtifactDigestAlgorithm = ArtifactDigestAlgorithm.None,
    string? ArtifactDigestHex = null,
    string? Title = null,
    string? Description = null,
    IReadOnlyList<string>? Authors = null,
    IReadOnlyList<string>? PackageTypes = null,
    string? ProjectUrl = null,
    string? RepositoryUrl = null,
    string? LicenseUrl = null,
    string? LicenseExpression = null,
    SpdxLicenseExpressionSyntax? LicenseExpressionSyntax = null,
    string? PackageDetailsUrl = null,
    IReadOnlyList<string>? Tags = null,
    DateTime? CreatedAt = null,
    DateTime? UpdatedAt = null);
