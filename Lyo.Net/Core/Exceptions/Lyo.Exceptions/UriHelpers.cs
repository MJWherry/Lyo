using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Lyo.Exceptions.Models;
#if NET6_0_OR_GREATER
using System.Diagnostics;

// ReSharper disable RedundantSuppressNullableWarningExpression
#endif

namespace Lyo.Exceptions;

/// <summary>Guards for URI validation and parsing.</summary>
/// <remarks>
/// Optional name parameters use <see cref="CallerArgumentExpressionAttribute" /> like <see cref="ArgumentHelpers" />: when omitted, the compiler fills
/// <see cref="ArgumentException.ParamName" />. Pass <c>nameof(...)</c> explicitly when preferred.
/// </remarks>
public static class UriHelpers
{
    [DoesNotReturn]
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    private static void ThrowInvalidFormat(string message, string? paramName, string? invalidValue, string expectedFormat)
        => throw new InvalidFormatException(message, paramName, invalidValue, expectedFormat);

    /// <summary>Throws InvalidFormatException when the URI string is null, empty, or not a valid URI.</summary>
    /// <param name="uri">URI string under validation.</param>
    /// <param name="paramName">Name written to ParamName.</param>
    /// <param name="uriKind">Kind of URI (Absolute, Relative, or RelativeOrAbsolute). Defaults to RelativeOrAbsolute.</param>
    /// <exception cref="ArgumentException">Thrown if uri is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown if uri is not a valid URI format.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfInvalidUri([NotNull] string? uri, [CallerArgumentExpression("uri")] string? paramName = null, UriKind uriKind = UriKind.RelativeOrAbsolute)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(uri, paramName);
        if (Uri.TryCreate(uri, uriKind, out var _))
            return;

        var expectedFormat = uriKind == UriKind.Absolute ? "Absolute URI (e.g., https://example.com)" :
            uriKind == UriKind.Relative ? "Relative URI (e.g., /path/to/resource)" : "Valid URI (absolute or relative)";

        ThrowInvalidFormat($"Invalid URI format: {uri}", paramName, uri, expectedFormat);
    }

    /// <summary>Throws InvalidFormatException when the URI string is null, empty, or not a valid absolute URI.</summary>
    /// <param name="uri">URI string under validation.</param>
    /// <param name="paramName">Name written to ParamName.</param>
    /// <exception cref="ArgumentException">Thrown if uri is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown if uri is not a valid absolute URI format.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfInvalidAbsoluteUri([NotNull] string? uri, [CallerArgumentExpression("uri")] string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(uri, paramName);
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var _))
            ThrowInvalidFormat($"Invalid absolute URI format: {uri}", paramName, uri, "Absolute URI (e.g., https://example.com)");
    }

    /// <summary>Validates a URI string and returns a valid Uri instance, or throws when invalid.</summary>
    /// <param name="uri">URI string under validation.</param>
    /// <param name="paramName">Name written to ParamName.</param>
    /// <param name="uriKind">Kind of URI (Absolute, Relative, or RelativeOrAbsolute). Defaults to Absolute.</param>
    /// <returns>A valid Uri instance.</returns>
    /// <exception cref="ArgumentException">Thrown if uri is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown if uri is not a valid URI format.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Uri GetValidUri([NotNull] string? uri, [CallerArgumentExpression("uri")] string? paramName = null, UriKind uriKind = UriKind.Absolute)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(uri, paramName);
        if (Uri.TryCreate(uri, uriKind, out var validUri))
            return validUri;

        var expectedFormat = uriKind switch {
            UriKind.Absolute => "Absolute URI (e.g., https://example.com)",
            UriKind.Relative => "Relative URI (e.g., /path/to/resource)",
            var _ => "Valid URI (absolute or relative)"
        };

        ThrowInvalidFormat($"Invalid URI format: {uri}", paramName, uri, expectedFormat);
        return null!; // Unreachable - ThrowInvalidFormat is [DoesNotReturn]
    }

    /// <summary>Attempts to parse a URI string and reports whether it is valid, with the parsed URI as an out parameter.</summary>
    /// <param name="uri">URI string under validation.</param>
    /// <param name="validUri">When this method returns, contains the parsed Uri on success; otherwise null.</param>
    /// <param name="paramName">Parameter name used in error messages.</param>
    /// <param name="uriKind">Kind of URI (Absolute, Relative, or RelativeOrAbsolute). Defaults to Absolute.</param>
    /// <returns>true when the URI was successfully parsed; otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetValidUri(string? uri, out Uri? validUri, string? paramName = null, UriKind uriKind = UriKind.Absolute)
    {
        validUri = null;
        return !string.IsNullOrWhiteSpace(uri) && Uri.TryCreate(uri, uriKind, out validUri);
    }

    /// <summary>Validates a URI string and returns a valid absolute Uri instance, or throws when invalid.</summary>
    /// <param name="uri">URI string under validation.</param>
    /// <param name="paramName">Name written to ParamName.</param>
    /// <returns>A valid absolute Uri instance.</returns>
    /// <exception cref="ArgumentException">Thrown if uri is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown if uri is not a valid absolute URI format.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Uri GetValidAbsoluteUri(string? uri, [CallerArgumentExpression("uri")] string? paramName = null) => GetValidUri(uri, paramName);

    /// <summary>Validates a URI string and returns a valid relative Uri instance, or throws when invalid.</summary>
    /// <param name="uri">URI string under validation.</param>
    /// <param name="paramName">Name written to ParamName.</param>
    /// <returns>A valid relative Uri instance.</returns>
    /// <exception cref="ArgumentException">Thrown if uri is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown if uri is not a valid relative URI format.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Uri GetValidRelativeUri(string? uri, [CallerArgumentExpression("uri")] string? paramName = null) => GetValidUri(uri, paramName, UriKind.Relative);

    /// <summary>Validates that a URI uses a specific scheme (for example http, https, ftp) and returns the Uri instance.</summary>
    /// <param name="uri">URI string under validation.</param>
    /// <param name="scheme">Required URI scheme (for example "http", "https", "ftp").</param>
    /// <param name="paramName">Name written to ParamName.</param>
    /// <returns>A valid Uri instance with the specified scheme.</returns>
    /// <exception cref="ArgumentException">Thrown if uri is null, empty, or whitespace, or when scheme is null or empty.</exception>
    /// <exception cref="InvalidFormatException">Thrown if uri is not a valid absolute URI or does not use the specified scheme.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Uri GetValidUriWithScheme(string? uri, string scheme, [CallerArgumentExpression("uri")] string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(scheme);
        var validUri = GetValidAbsoluteUri(uri, paramName);
        if (!string.Equals(validUri.Scheme, scheme, StringComparison.OrdinalIgnoreCase))
            ThrowInvalidFormat($"URI must use the '{scheme}' scheme. Found: {validUri.Scheme}", paramName, uri!, $"{scheme}://...");

        return validUri;
    }

    /// <summary>Validates that a URI uses the HTTP or HTTPS scheme and returns the Uri instance.</summary>
    /// <param name="uri">URI string under validation.</param>
    /// <param name="paramName">Name written to ParamName.</param>
    /// <returns>A valid Uri instance with an HTTP or HTTPS scheme.</returns>
    /// <exception cref="ArgumentException">Thrown if uri is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown if uri is not a valid absolute URI or does not use the HTTP/HTTPS scheme.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Uri GetValidWebUri(string? uri, [CallerArgumentExpression("uri")] string? paramName = null)
    {
        var validUri = GetValidAbsoluteUri(uri, paramName);
        if (validUri.Scheme != Uri.UriSchemeHttp && validUri.Scheme != Uri.UriSchemeHttps)
            ThrowInvalidFormat($"URI must use HTTP or HTTPS scheme. Found: {validUri.Scheme}", paramName, uri!, "http://... or https://...");

        return validUri;
    }

    /// <summary>Validates that a URI string is a valid absolute URI and throws InvalidFormatException if not. Convenience wrapper around ThrowIfInvalidAbsoluteUri.</summary>
    /// <param name="uri">URI string under validation.</param>
    /// <param name="paramName">Name written to ParamName.</param>
    /// <exception cref="ArgumentException">Thrown if uri is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown if uri is not a valid absolute URI format.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ValidateAbsoluteUri(string? uri, [CallerArgumentExpression("uri")] string? paramName = null) => ThrowIfInvalidAbsoluteUri(uri, paramName);

    /// <summary>Validates that a URI string is a valid URI (absolute or relative) and throws InvalidFormatException if not. Convenience wrapper around ThrowIfInvalidUri.</summary>
    /// <param name="uri">URI string under validation.</param>
    /// <param name="paramName">Name written to ParamName.</param>
    /// <exception cref="ArgumentException">Thrown if uri is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown if uri is not a valid URI format.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ValidateUri(string? uri, [CallerArgumentExpression("uri")] string? paramName = null) => ThrowIfInvalidUri(uri, paramName);

    /// <summary>Combines a base URI with a path segment, handling trailing and leading slashes.</summary>
    /// <param name="baseUri">Base URI (for example https://api.example.com).</param>
    /// <param name="path">Path to append (for example /users or users).</param>
    /// <param name="paramName">Parameter name used in error messages.</param>
    /// <returns>Combined URI string.</returns>
    /// <exception cref="ArgumentException">Thrown if baseUri is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown if baseUri is not a valid absolute URI.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CombineUri(string? baseUri, string? path, [CallerArgumentExpression("baseUri")] string? paramName = null)
    {
        var validBase = GetValidAbsoluteUri(baseUri, paramName);
        if (string.IsNullOrEmpty(path))
            return validBase.ToString();

        var trimPath = path!.TrimStart('/');
        var baseStr = validBase.ToString().TrimEnd('/');
        return string.IsNullOrEmpty(trimPath) ? baseStr : $"{baseStr}/{trimPath}";
    }

    /// <summary>Appends a query string to a URI, adding ? or &amp; as appropriate.</summary>
    /// <param name="uri">Base URI.</param>
    /// <param name="queryString">Query string to append (without a leading ?).</param>
    /// <returns>URI with the query string appended.</returns>
    /// <exception cref="ArgumentException">Thrown if uri is null, empty, or whitespace.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AppendQueryString(string? uri, string? queryString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(uri);
        if (string.IsNullOrEmpty(queryString))
            return uri;

        var trimmed = uri.TrimEnd('?', '&');
        var hasQuery = trimmed.Contains('?');
        return $"{trimmed}{(hasQuery ? "&" : "?")}{queryString!.TrimStart('?', '&')}";
    }

    /// <summary>Attempts to combine a base URI with a path. Returns false when baseUri is invalid.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryCombineUri(string? baseUri, string? path, out string? combined)
    {
        combined = null;
        if (string.IsNullOrWhiteSpace(baseUri) || !Uri.TryCreate(baseUri, UriKind.Absolute, out var validBase))
            return false;

        if (string.IsNullOrEmpty(path))
            combined = validBase.ToString();
        else {
            var trimPath = path!.TrimStart('/');
            var baseStr = validBase.ToString().TrimEnd('/');
            combined = string.IsNullOrEmpty(trimPath) ? baseStr : $"{baseStr}/{trimPath}";
        }

        return true;
    }
}