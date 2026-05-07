namespace Lyo.PackageMetadata;

/// <summary>
/// JSON-friendly SPDX 2.x license expression shape: binary <see cref="Kind" /> values <c>and</c> / <c>or</c>, unary <c>with</c> using <see cref="InnerLicense" /> and
/// <see cref="InnerException" />, and leaves <c>license</c> / <c>exception</c>.
/// </summary>
/// <param name="Kind"><c>license</c>, <c>exception</c>, <c>and</c>, <c>or</c>, or <c>with</c>.</param>
public sealed record SpdxLicenseExpressionSyntax(
    string Kind,
    string? Identifier = null,
    bool? PlusSuffix = null,
    SpdxLicenseExpressionSyntax? Left = null,
    SpdxLicenseExpressionSyntax? Right = null,
    SpdxLicenseExpressionSyntax? InnerLicense = null,
    SpdxLicenseExpressionSyntax? InnerException = null);