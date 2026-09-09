using System.Collections;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Extensions;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Common.Core;

/// <summary>
/// Shared helpers: file-size conversion (powers of 1024), URI redaction, type classification, SHA256 hashing, and LINQ expression property name/path extraction.
/// </summary>
public static class Utilities
{
    private static readonly Regex SensitiveUriRegex = new(@"(?i:(?<!result|status)(\w*secret|\w*token|\bcode\b|\w*password))(?:\s|=|:)(.*?)(?:\s|&)");

    /// <summary>Converts a byte count into the requested unit using binary (1024-based) steps.</summary>
    /// <param name="bytes">Size in bytes; must not be negative.</param>
    /// <param name="targetUnit">Unit to convert to (each step is a factor of 1024).</param>
    /// <returns>Size expressed in <paramref name="targetUnit" />.</returns>
    /// <exception cref="ArgumentOutsideRangeException"><paramref name="bytes" /> is negative.</exception>
    public static double ConvertFromBytes(long bytes, FileSizeUnit targetUnit)
    {
        ArgumentHelpers.ThrowIfNegative(bytes);
        var power = (int)targetUnit;
        return bytes / Math.Pow(1024, power);
    }

    /// <summary>Converts a scalar size in the given unit to whole bytes using binary (1024-based) steps.</summary>
    /// <param name="size">Numeric size; must not be negative.</param>
    /// <param name="sourceUnit">Unit of <paramref name="size" />.</param>
    /// <returns>Equivalent size in bytes, truncated toward zero.</returns>
    /// <exception cref="ArgumentOutsideRangeException"><paramref name="size" /> is negative.</exception>
    public static long ConvertToBytes(double size, FileSizeUnit sourceUnit)
    {
        ArgumentHelpers.ThrowIfNegative(size);
        var power = (int)sourceUnit;
        return (long)(size * Math.Pow(1024, power));
    }

    /// <summary>Converts a size between two file-size units via bytes (binary / 1024).</summary>
    /// <param name="size">Numeric size in <paramref name="sourceUnit" />; must not be negative.</param>
    /// <param name="sourceUnit">Unit of <paramref name="size" />.</param>
    /// <param name="targetUnit">Desired output unit.</param>
    /// <returns>Size expressed in <paramref name="targetUnit" />.</returns>
    /// <exception cref="ArgumentOutsideRangeException"><paramref name="size" /> is negative.</exception>
    public static double Convert(double size, FileSizeUnit sourceUnit, FileSizeUnit targetUnit)
    {
        var bytes = ConvertToBytes(size, sourceUnit);
        return ConvertFromBytes(bytes, targetUnit);
    }

    /// <summary>
    /// Redacts likely-sensitive query or fragment segments (keys matching <c>*secret</c>, <c>*token</c>, <c>*password</c>, or <c>code</c>) by replacing captured values with
    /// asterisks.
    /// </summary>
    /// <param name="uri">URI string to sanitize, or <see langword="null" />.</param>
    /// <returns>Trimmed string with sensitive-looking assignments redacted, or <see langword="null" /> when the input is null or empty after trim.</returns>
    /// <remarks>Matching is heuristic; pairs named <c>result</c> or <c>status</c> are left out of the secret/token pattern prefix.</remarks>
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

    /// <summary>True when <paramref name="type" /> is treated as a collection for serialization-style checks (enumerable but not <see cref="string" />).</summary>
    /// <param name="type">CLR type to inspect.</param>
    /// <returns><see langword="true" /> if <paramref name="type" /> implements <see cref="IEnumerable" /> and is not <see cref="string" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCollectionType(Type type) => type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);

    /// <summary>True when <paramref name="type" /> is a scalar (delegates to <see cref="LyoReflection" /> <c>IsScalar</c>).</summary>
    /// <param name="type">CLR type to inspect.</param>
    /// <returns><see langword="true" /> if the type is classified as scalar.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsScalarType(Type type) => type.IsScalar();

    /// <summary>SHA256 digest of a file.</summary>
    /// <param name="path">Path to an existing file.</param>
    /// <returns>The 32-byte SHA256 digest.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="path" /> is null, empty, or not a valid relative URI per <see cref="ArgumentHelpers.ThrowIfFileNotFound(string)" />.
    /// </exception>
    /// <exception cref="InvalidFormatException"><paramref name="path" /> fails URI validation.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    public static byte[] Hash(string path)
    {
        ArgumentHelpers.ThrowIfFileNotFound(path);
        using var stream = File.OpenRead(path);
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(stream);
    }

    /// <summary>SHA256 digest of a byte buffer.</summary>
    /// <param name="input">Data to hash.</param>
    /// <returns>The 32-byte SHA256 digest.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input" /> is <see langword="null" />.</exception>
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

    /// <summary>SHA256 digest of a readable stream, asynchronously when the platform supports it.</summary>
    /// <param name="stream">Stream to read; must be readable.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes with the 32-byte SHA256 digest.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="stream" /> is not readable.</exception>
    /// <remarks>Below .NET 6, hashing runs synchronously after a yield; cancellation is only observed before that step.</remarks>
    public static async Task<byte[]> HashAsync(Stream stream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        OperationHelpers.ThrowIfNotReadable(stream, $"Stream '{nameof(stream)}' must be readable.");
        using var sha256 = SHA256.Create();
#if NET6_0_OR_GREATER
        return await sha256.ComputeHashAsync(stream, ct).ConfigureAwait(false);
#else
        // .NET Standard 2.0 has no ComputeHashAsync; hash synchronously after Yield
        await Task.Yield();
        ct.ThrowIfCancellationRequested();
        return sha256.ComputeHash(stream);
#endif
    }

    /// <summary>Opens the file and hashes it asynchronously (delegates to <see cref="HashAsync(Stream, CancellationToken)" />).</summary>
    /// <param name="path">Path to an existing file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes with the 32-byte SHA256 digest.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="path" /> is null, empty, or not a valid relative URI per <see cref="ArgumentHelpers.ThrowIfFileNotFound(string)" />.
    /// </exception>
    /// <exception cref="InvalidFormatException"><paramref name="path" /> fails URI validation.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
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

    /// <summary>Leaf property name from a simple member-access lambda (for example <c>x => x.FirstName</c> → <c>FirstName</c>).</summary>
    /// <typeparam name="T">Declaring type.</typeparam>
    /// <typeparam name="TProperty">Property type.</typeparam>
    /// <param name="expression">Lambda that accesses a single property, optionally wrapped in a conversion.</param>
    /// <returns>Member name of the accessed property.</returns>
    /// <exception cref="ArgumentException"><paramref name="expression" /> is not a property access expression.</exception>
    public static string GetPropertyName<T, TProperty>(Expression<Func<T, TProperty>> expression) => expression.GetMemberName();

    /// <summary>Dotted path for nested property access (for example <c>x => x.Address.Street</c> → <c>Address.Street</c>).</summary>
    /// <typeparam name="T">Root declaring type.</typeparam>
    /// <typeparam name="TProperty">Leaf property type.</typeparam>
    /// <param name="expression">Lambda composed only of member accesses from <typeparamref name="T" /> to the leaf property.</param>
    /// <returns>Dot-separated property path from root to leaf.</returns>
    /// <exception cref="ArgumentException"><paramref name="expression" /> is not a chain of property accesses.</exception>
    public static string GetPropertyPath<T, TProperty>(Expression<Func<T, TProperty>> expression) => expression.GetMemberPath();
}