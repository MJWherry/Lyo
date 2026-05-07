using System.Collections;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Lyo.Common.Enums;
using Lyo.Common.Extensions;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Common;

/// <summary>General-purpose helpers: file-size conversion (powers of 1024), URI redaction, type classification, SHA256 hashing, and LINQ expression property name/path extraction.</summary>
public static class Utilities
{
    private static readonly HashSet<Type> ScalarTypes = [typeof(string), typeof(decimal), typeof(Guid), typeof(DateTime), typeof(TimeSpan)];
    private static readonly Regex SensitiveUriRegex = new(@"(?i:(?<!result|status)(\w*secret|\w*token|\bcode\b|\w*password))(?:\s|=|:)(.*?)(?:\s|&)");

    /// <summary>Converts a byte count into the requested unit using binary (1024-based) steps.</summary>
    /// <param name="bytes">The size in bytes; must not be negative.</param>
    /// <param name="targetUnit">The unit to convert to (each step is a factor of 1024).</param>
    /// <returns>The size expressed in <paramref name="targetUnit" />.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when <paramref name="bytes" /> is negative.</exception>
    public static double ConvertFromBytes(long bytes, FileSizeUnit targetUnit)
    {
        ArgumentHelpers.ThrowIfNegative(bytes);
        var power = (int)targetUnit;
        return bytes / Math.Pow(1024, power);
    }

    /// <summary>Converts a scalar size in the given unit to whole bytes using binary (1024-based) steps.</summary>
    /// <param name="size">The numeric size; must not be negative.</param>
    /// <param name="sourceUnit">The unit of <paramref name="size" />.</param>
    /// <returns>The equivalent size in bytes, truncated toward zero.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when <paramref name="size" /> is negative.</exception>
    public static long ConvertToBytes(double size, FileSizeUnit sourceUnit)
    {
        ArgumentHelpers.ThrowIfNegative(size);
        var power = (int)sourceUnit;
        return (long)(size * Math.Pow(1024, power));
    }

    /// <summary>Converts a size between two file-size units via bytes (binary / 1024).</summary>
    /// <param name="size">The numeric size in <paramref name="sourceUnit" />; must not be negative.</param>
    /// <param name="sourceUnit">The unit of <paramref name="size" />.</param>
    /// <param name="targetUnit">The desired output unit.</param>
    /// <returns>The size expressed in <paramref name="targetUnit" />.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when <paramref name="size" /> is negative.</exception>
    public static double Convert(double size, FileSizeUnit sourceUnit, FileSizeUnit targetUnit)
    {
        var bytes = ConvertToBytes(size, sourceUnit);
        return ConvertFromBytes(bytes, targetUnit);
    }

    /// <summary>
    /// Redacts likely-sensitive query or fragment segments (e.g. keys matching <c>*secret</c>, <c>*token</c>, <c>*password</c>, or <c>code</c>) by replacing captured values with
    /// asterisks.
    /// </summary>
    /// <param name="uri">The URI string to sanitize, or <see langword="null" />.</param>
    /// <returns>A trimmed string with sensitive-looking assignments redacted, or <see langword="null" /> when the input is null or empty after trim.</returns>
    /// <remarks>Matching is heuristic; pairs named <c>result</c> or <c>status</c> are excluded from the secret/token pattern prefix.</remarks>
    public static string? SanitizeUri(string? uri)
    {
        if (uri is null)
            return null;

        var sanitizedQueryString = uri.Trim();
        if (sanitizedQueryString.IsNullOrEmpty())
            return null;

        var matches = SensitiveUriRegex.Matches(sanitizedQueryString);
        foreach (Match match in matches)
            sanitizedQueryString = sanitizedQueryString.Replace(match.Groups[2].Value, "********");

        return sanitizedQueryString;
    }

    /// <summary>Determines whether <paramref name="type" /> is considered a collection for serialization-style checks (enumerable but not <see cref="string" />).</summary>
    /// <param name="type">The CLR type to inspect.</param>
    /// <returns><see langword="true" /> if <paramref name="type" /> implements <see cref="IEnumerable" /> and is not <see cref="string" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCollectionType(Type type) => type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);

    /// <summary>
    /// Determines whether <paramref name="type" /> is treated as a scalar (primitive, enum, or a small fixed set including <see cref="string" />, <see cref="decimal" />,
    /// <see cref="Guid" />, <see cref="DateTime" />, <see cref="TimeSpan" />).
    /// </summary>
    /// <param name="type">The CLR type to inspect.</param>
    /// <returns><see langword="true" /> if the type is classified as scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsScalarType(Type type) => type.IsPrimitive || type.IsEnum || ScalarTypes.Contains(type);

    /// <summary>Computes the SHA256 hash of a file.</summary>
    /// <param name="path">Path to an existing file.</param>
    /// <returns>The 32-byte SHA256 digest.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="path" /> is null, empty, or not a valid relative URI per <see cref="ArgumentHelpers.ThrowIfFileNotFound(string)" />
    /// .
    /// </exception>
    /// <exception cref="InvalidFormatException">Thrown when <paramref name="path" /> fails URI validation.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public static byte[] Hash(string path)
    {
        ArgumentHelpers.ThrowIfFileNotFound(path);
        using var stream = File.OpenRead(path);
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(stream);
    }

    /// <summary>Computes the SHA256 hash of a byte buffer.</summary>
    /// <param name="input">The data to hash.</param>
    /// <returns>The 32-byte SHA256 digest.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input" /> is <see langword="null" />.</exception>
    public static byte[] Hash(byte[] input)
    {
        ArgumentHelpers.ThrowIfNull(input);
#if NETSTANDARD2_0
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(input);
#else
        return SHA256.HashData(input);
#endif
    }

    /// <summary>Computes the SHA256 hash of a readable stream asynchronously when supported by the platform.</summary>
    /// <param name="stream">The stream to read from; must be readable.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes with the 32-byte SHA256 digest.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="stream" /> is not readable.</exception>
    /// <remarks>On targets below .NET 6, hashing runs synchronously after a yield; cancellation is only observed before that step.</remarks>
    public static async Task<byte[]> HashAsync(Stream stream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        OperationHelpers.ThrowIfNotReadable(stream, $"Stream '{nameof(stream)}' must be readable.");
        using var sha256 = SHA256.Create();
#if NET6_0_OR_GREATER
        return await sha256.ComputeHashAsync(stream, ct).ConfigureAwait(false);
#else
        // .NET Standard 2.0: ComputeHashAsync doesn't exist, use synchronous version
        await Task.Yield();
        ct.ThrowIfCancellationRequested();
        return sha256.ComputeHash(stream);
#endif
    }

    /// <summary>Opens the file and computes its SHA256 hash asynchronously (delegates to <see cref="HashAsync(Stream, CancellationToken)" />).</summary>
    /// <param name="path">Path to an existing file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes with the 32-byte SHA256 digest.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="path" /> is null, empty, or not a valid relative URI per <see cref="ArgumentHelpers.ThrowIfFileNotFound(string)" />
    /// .
    /// </exception>
    /// <exception cref="InvalidFormatException">Thrown when <paramref name="path" /> fails URI validation.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public static async Task<byte[]> HashAsync(string path, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(path);
#if NET6_0_OR_GREATER
        await using var stream = File.OpenRead(path);
#else
        using var stream = File.OpenRead(path);
#endif
        return await HashAsync(stream, ct).ConfigureAwait(false);
    }

    /// <summary>Extracts the leaf property name from a simple member-access lambda (e.g. <c>x => x.FirstName</c> → <c>FirstName</c>).</summary>
    /// <typeparam name="T">The declaring type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="expression">A lambda that accesses a single property, optionally wrapped in a conversion.</param>
    /// <returns>The member name of the accessed property.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="expression" /> is not a property access expression.</exception>
    public static string GetPropertyName<T, TProperty>(Expression<Func<T, TProperty>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
            return memberExpression.Member.Name;

        if (expression.Body is UnaryExpression unaryExpression) {
            var operand = unaryExpression.Operand as MemberExpression;
            if (operand != null)
                return operand.Member.Name;
        }

        ArgumentHelpers.ThrowIf(true, "Invalid expression. Must be a property access expression.");
        return null!;
    }

    /// <summary>Builds a dotted path for nested property access (e.g. <c>x => x.Address.Street</c> → <c>Address.Street</c>).</summary>
    /// <typeparam name="T">The root declaring type.</typeparam>
    /// <typeparam name="TProperty">The leaf property type.</typeparam>
    /// <param name="expression">A lambda composed only of member accesses from <typeparamref name="T" /> to the leaf property.</param>
    /// <returns>The dot-separated property path from root to leaf.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="expression" /> is not a chain of property accesses.</exception>
    public static string GetPropertyPath<T, TProperty>(Expression<Func<T, TProperty>> expression)
    {
        var path = string.Empty;
        var currentExpression = expression.Body;
        while (currentExpression is MemberExpression memberExpression) {
            path = memberExpression.Member.Name + (string.IsNullOrEmpty(path) ? "" : "." + path);
            currentExpression = memberExpression.Expression;
        }

        ArgumentHelpers.ThrowIf(path.IsNullOrEmpty(), "Invalid expression. Must be a property access expression.");
        return path;
    }
}