using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Lyo.Exceptions.Models;

namespace Lyo.Exceptions;

/// <summary>Helper methods for URI validation and parsing.</summary>
public static class UriHelpers
{
#if NET6_0_OR_GREATER
    [DoesNotReturn]
    [StackTraceHidden]
#endif
    private static void ThrowInvalidFormat(string message, string? paramName, string? invalidValue, string expectedFormat)
        => throw new InvalidFormatException(message, paramName, invalidValue, expectedFormat);

    /// <summary>Throws an InvalidFormatException if the URI string is null, empty, or not a valid URI.</summary>
    /// <param name="uri">The URI string to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <param name="uriKind">The kind of URI (Absolute, Relative, or RelativeOrAbsolute). Default is RelativeOrAbsolute.</param>
    /// <exception cref="ArgumentException">Thrown when uri is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown when uri is not a valid URI format.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static void ThrowIfInvalidUri([NotNull] string? uri, string? paramName = null, UriKind uriKind = UriKind.RelativeOrAbsolute)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(uri, paramName ?? nameof(uri));
        if (Uri.TryCreate(uri!, uriKind, out var _))
            return;

        var expectedFormat = uriKind == UriKind.Absolute ? "Absolute URI (e.g., https://example.com)" :
            uriKind == UriKind.Relative ? "Relative URI (e.g., /path/to/resource)" : "Valid URI (absolute or relative)";

        ThrowInvalidFormat($"Invalid URI format: {uri}", paramName ?? nameof(uri), uri!, expectedFormat);
    }

    /// <summary>Throws an InvalidFormatException if the URI string is null, empty, or not a valid absolute URI.</summary>
    /// <param name="uri">The URI string to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when uri is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown when uri is not a valid absolute URI format.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static void ThrowIfInvalidAbsoluteUri([NotNull] string? uri, string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(uri, paramName ?? nameof(uri));
        if (!Uri.TryCreate(uri!, UriKind.Absolute, out var _))
            ThrowInvalidFormat($"Invalid absolute URI format: {uri}", paramName ?? nameof(uri), uri!, "Absolute URI (e.g., https://example.com)");
    }

    /// <summary>Validates a URI string and returns a valid Uri instance, or throws an exception if invalid.</summary>
    /// <param name="uri">The URI string to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <param name="uriKind">The kind of URI (Absolute, Relative, or RelativeOrAbsolute). Default is Absolute.</param>
    /// <returns>A valid Uri instance.</returns>
    /// <exception cref="ArgumentException">Thrown when uri is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown when uri is not a valid URI format.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static Uri GetValidUri([NotNull] string? uri, string? paramName = null, UriKind uriKind = UriKind.Absolute)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(uri, paramName ?? nameof(uri));
        if (Uri.TryCreate(uri!, uriKind, out var validUri))
            return validUri;

        var expectedFormat = uriKind switch {
            UriKind.Absolute => "Absolute URI (e.g., https://example.com)",
            UriKind.Relative => "Relative URI (e.g., /path/to/resource)",
            var _ => "Valid URI (absolute or relative)"
        };

        ThrowInvalidFormat($"Invalid URI format: {uri}", paramName ?? nameof(uri), uri!, expectedFormat);
        return null!; // Unreachable - ThrowInvalidFormat is [DoesNotReturn]
    }

    /// <summary>Attempts to parse a URI string and returns whether it is valid, with the parsed URI as an out parameter.</summary>
    /// <param name="uri">The URI string to validate.</param>
    /// <param name="validUri">When this method returns, contains the parsed Uri if successful; otherwise, null.</param>
    /// <param name="paramName">The parameter name for error messages.</param>
    /// <param name="uriKind">The kind of URI (Absolute, Relative, or RelativeOrAbsolute). Default is Absolute.</param>
    /// <returns>true if the URI was successfully parsed; otherwise, false.</returns>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static bool TryGetValidUri(string? uri, out Uri? validUri, string? paramName = null, UriKind uriKind = UriKind.Absolute)
    {
        validUri = null;
        return !string.IsNullOrWhiteSpace(uri) && Uri.TryCreate(uri, uriKind, out validUri);
    }

    /// <summary>Validates a URI string and returns a valid absolute Uri instance, or throws an exception if invalid.</summary>
    /// <param name="uri">The URI string to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <returns>A valid absolute Uri instance.</returns>
    /// <exception cref="ArgumentException">Thrown when uri is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown when uri is not a valid absolute URI format.</exception>
    public static Uri GetValidAbsoluteUri(string? uri, string? paramName = null) => GetValidUri(uri, paramName);

    /// <summary>Validates a URI string and returns a valid relative Uri instance, or throws an exception if invalid.</summary>
    /// <param name="uri">The URI string to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <returns>A valid relative Uri instance.</returns>
    /// <exception cref="ArgumentException">Thrown when uri is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown when uri is not a valid relative URI format.</exception>
    public static Uri GetValidRelativeUri(string? uri, string? paramName = null) => GetValidUri(uri, paramName, UriKind.Relative);

    /// <summary>Validates that a URI uses a specific scheme (e.g., http, https, ftp) and returns the Uri instance.</summary>
    /// <param name="uri">The URI string to validate.</param>
    /// <param name="scheme">The required URI scheme (e.g., "http", "https", "ftp").</param>
    /// <param name="paramName">The parameter name.</param>
    /// <returns>A valid Uri instance with the specified scheme.</returns>
    /// <exception cref="ArgumentException">Thrown when uri is null, empty, or whitespace, or when scheme is null or empty.</exception>
    /// <exception cref="InvalidFormatException">Thrown when uri is not a valid absolute URI or does not use the specified scheme.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static Uri GetValidUriWithScheme(string? uri, string scheme, string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(scheme, nameof(scheme));
        var validUri = GetValidAbsoluteUri(uri, paramName);
        if (!string.Equals(validUri.Scheme, scheme, StringComparison.OrdinalIgnoreCase))
            ThrowInvalidFormat($"URI must use the '{scheme}' scheme. Found: {validUri.Scheme}", paramName ?? nameof(uri), uri!, $"{scheme}://...");

        return validUri;
    }

    /// <summary>Validates that a URI uses HTTP or HTTPS scheme and returns the Uri instance.</summary>
    /// <param name="uri">The URI string to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <returns>A valid Uri instance with HTTP or HTTPS scheme.</returns>
    /// <exception cref="ArgumentException">Thrown when uri is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown when uri is not a valid absolute URI or does not use HTTP/HTTPS scheme.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static Uri GetValidWebUri(string? uri, string? paramName = null)
    {
        var validUri = GetValidAbsoluteUri(uri, paramName);
        if (validUri.Scheme != Uri.UriSchemeHttp && validUri.Scheme != Uri.UriSchemeHttps)
            ThrowInvalidFormat($"URI must use HTTP or HTTPS scheme. Found: {validUri.Scheme}", paramName ?? nameof(uri), uri!, "http://... or https://...");

        return validUri;
    }

    /// <summary>Validates that a URI string is a valid absolute URI and throws InvalidFormatException if not. This is a convenience method that calls ThrowIfInvalidAbsoluteUri.</summary>
    /// <param name="uri">The URI string to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when uri is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown when uri is not a valid absolute URI format.</exception>
    public static void ValidateAbsoluteUri(string? uri, string? paramName = null) => ThrowIfInvalidAbsoluteUri(uri, paramName);

    /// <summary>Validates that a URI string is a valid URI (absolute or relative) and throws InvalidFormatException if not. This is a convenience method that calls ThrowIfInvalidUri.</summary>
    /// <param name="uri">The URI string to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when uri is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown when uri is not a valid URI format.</exception>
    public static void ValidateUri(string? uri, string? paramName = null) => ThrowIfInvalidUri(uri, paramName);

    /// <summary>Combines a base URI with a path segment, handling trailing/leading slashes.</summary>
    /// <param name="baseUri">The base URI (e.g. https://api.example.com).</param>
    /// <param name="path">The path to append (e.g. /users or users).</param>
    /// <param name="paramName">The parameter name for error messages.</param>
    /// <returns>A combined URI string.</returns>
    /// <exception cref="ArgumentException">Thrown when baseUri is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown when baseUri is not a valid absolute URI.</exception>
    public static string CombineUri(string? baseUri, string? path, string? paramName = null)
    {
        var validBase = GetValidAbsoluteUri(baseUri, paramName ?? nameof(baseUri));
        if (string.IsNullOrEmpty(path))
            return validBase.ToString();

        var trimPath = path!.TrimStart('/');
        var baseStr = validBase.ToString().TrimEnd('/');
        return string.IsNullOrEmpty(trimPath) ? baseStr : $"{baseStr}/{trimPath}";
    }

    /// <summary>Appends a query string to a URI, adding ? or & as appropriate.</summary>
    /// <param name="uri">The base URI.</param>
    /// <param name="queryString">The query string to append (without leading ?).</param>
    /// <returns>The URI with the query string appended.</returns>
    /// <exception cref="ArgumentException">Thrown when uri is null, empty, or whitespace.</exception>
    public static string AppendQueryString(string? uri, string? queryString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(uri, nameof(uri));
        if (string.IsNullOrEmpty(queryString))
            return uri!;

        var trimmed = uri!.TrimEnd('?', '&');
        var hasQuery = trimmed.IndexOf('?') >= 0;
        return $"{trimmed}{(hasQuery ? "&" : "?")}{queryString!.TrimStart('?', '&')}";
    }

    /// <summary>Attempts to combine a base URI with a path. Returns false if baseUri is invalid.</summary>
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