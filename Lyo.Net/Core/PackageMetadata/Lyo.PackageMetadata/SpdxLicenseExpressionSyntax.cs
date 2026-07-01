namespace Lyo.PackageMetadata;

/// <summary>
/// JSON-friendly SPDX 2.x license expression shape: binary <see cref="Kind" /> values <c>and</c> / <c>or</c>, unary <c>with</c> using <see cref="InnerLicense" /> and
/// <see cref="InnerException" />, and leaves <c>license</c> / <c>exception</c>.
/// </summary>
/// <param name="Kind"><c>license</c>, <c>exception</c>, <c>and</c>, <c>or</c>, or <c>with</c>.</param>
/// <param name="Identifier">SPDX license or exception id for leaf nodes (<c>license</c> / <c>exception</c>); <see langword="null" /> for operator nodes.</param>
/// <param name="PlusSuffix"><see langword="true" /> when a leaf license carries the SPDX <c>+</c> ("or later") suffix; <see langword="null" /> when not applicable.</param>
/// <param name="Left">Left operand for binary <c>and</c> / <c>or</c> nodes.</param>
/// <param name="Right">Right operand for binary <c>and</c> / <c>or</c> nodes.</param>
/// <param name="InnerLicense">The license operand of a <c>with</c> node.</param>
/// <param name="InnerException">The exception operand of a <c>with</c> node.</param>
public sealed record SpdxLicenseExpressionSyntax(
    string Kind,
    string? Identifier = null,
    bool? PlusSuffix = null,
    SpdxLicenseExpressionSyntax? Left = null,
    SpdxLicenseExpressionSyntax? Right = null,
    SpdxLicenseExpressionSyntax? InnerLicense = null,
    SpdxLicenseExpressionSyntax? InnerException = null);