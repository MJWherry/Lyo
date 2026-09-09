namespace Lyo.PackageMetadata;

/// <summary>
/// Catalog metadata for a published artifact, meant for JSON and database persistence. Several rows may share the same <see cref="Name" /> with different
/// <see cref="Version" /> values. How <see cref="Name" /> / <see cref="Version" /> are read depends on <see cref="Ecosystem" /> (for example NuGet id + version; Maven often
/// uses <c>groupId:artifactId</c> as <see cref="Name" />).
/// </summary>
/// <param name="Id">Primary key when this row is stored (for example a Postgres <c>uuid</c>).</param>
/// <param name="Ecosystem">Registry or distribution family for this row.</param>
/// <param name="Name">Package identity (for example a NuGet id, or Maven <c>groupId:artifactId</c>).</param>
/// <param name="Version">Published version string when known (ecosystem-specific format).</param>
/// <param name="ArtifactDigestAlgorithm">Hash algorithm for <see cref="ArtifactDigestHex" />; <see cref="ArtifactDigestAlgorithm.None" /> when no digest is stored.</param>
/// <param name="ArtifactDigestHex">Lowercase hex digest of the canonical primary artifact (40 chars SHA-1, 64 SHA-256, 128 SHA-512), or <see langword="null" />.</param>
/// <param name="Title">Human-readable display title for the package, when available.</param>
/// <param name="Description">Package description or summary, when available.</param>
/// <param name="Authors">Declared authors or maintainers, when available.</param>
/// <param name="PackageTypes">Ecosystem-specific type classifiers (for example NuGet package types), when available.</param>
/// <param name="ProjectUrl">Canonical project or homepage URL, when available.</param>
/// <param name="RepositoryUrl">Source repository URL, when available.</param>
/// <param name="LicenseUrl">License document URL, when available. Prefer <paramref name="LicenseExpression" /> as the canonical license field.</param>
/// <param name="LicenseExpression">When known, an SPDX 2.x license expression: <c>AND</c>, <c>OR</c>, parentheses, <c>WITH</c>. Canonical license field.</param>
/// <param name="LicenseExpressionSyntax">
/// Parsed tree of <see cref="LicenseExpression" /> (surjective with the string when parsing succeeds). Filled when persisting or loading; not
/// an independent legal source — use <see cref="LicenseExpression" /> as written.
/// </param>
/// <param name="PackageDetailsUrl">URL of the package details page on its registry, when available.</param>
/// <param name="Tags">Free-form tags or keywords for the package, when available.</param>
/// <param name="CreatedAt">When this metadata row was first stored (UTC). Set by the Postgres store; optional elsewhere.</param>
/// <param name="UpdatedAt">When this metadata row was last updated (UTC). Set by the Postgres store; optional elsewhere.</param>
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