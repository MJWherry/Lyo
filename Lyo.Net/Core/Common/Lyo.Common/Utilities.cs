using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Lyo.Common.Enums;
using Lyo.Exceptions;

namespace Lyo.Common;

public static class Utilities
{
    private static readonly HashSet<Type> ScalarTypes = [typeof(string), typeof(decimal), typeof(Guid), typeof(DateTime), typeof(TimeSpan)];
    private static readonly Regex SensitiveUriRegex = new(@"(?i:(?<!result|status)(\w*secret|\w*token|\bcode\b|\w*password))(?:\s|=|:)(.*?)(?:\s|&)");

    public static double ConvertFromBytes(long bytes, FileSizeUnit targetUnit)
    {
        ArgumentHelpers.ThrowIfNegative(bytes);
        var power = (int)targetUnit;
        return bytes / Math.Pow(1024, power);
    }

    public static long ConvertToBytes(double size, FileSizeUnit sourceUnit)
    {
        ArgumentHelpers.ThrowIfNegative(size);
        var power = (int)sourceUnit;
        return (long)(size * Math.Pow(1024, power));
    }

    public static double Convert(double size, FileSizeUnit sourceUnit, FileSizeUnit targetUnit)
    {
        var bytes = ConvertToBytes(size, sourceUnit);
        return ConvertFromBytes(bytes, targetUnit);
    }

    public static string? SanitizeUri(string? uri)
    {
        if (uri is null)
            return null;

        var sanitizedQueryString = uri.Trim();
        if (string.IsNullOrEmpty(sanitizedQueryString))
            return null;

        var matches = SensitiveUriRegex.Matches(sanitizedQueryString);
        foreach (Match match in matches)
            sanitizedQueryString = sanitizedQueryString.Replace(match.Groups[2].Value, "********");

        return sanitizedQueryString;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCollectionType(Type type) => type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsScalarType(Type type) => type.IsPrimitive || type.IsEnum || ScalarTypes.Contains(type);

    [return: NotNull]
    public static byte[] Hash([NotNull] string path)
    {
        ArgumentHelpers.ThrowIfFileNotFound(path);
        using var stream = File.OpenRead(path);
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(stream);
    }

    [return: NotNull]
    public static byte[] Hash([NotNull] byte[] input)
    {
        ArgumentHelpers.ThrowIfNull(input);
#if NETSTANDARD2_0
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(input);
#else
        return SHA256.HashData(input);
#endif
    }

    /// <summary>Computes SHA256 hash of a stream asynchronously.</summary>
    [return: NotNull]
    public static async Task<byte[]> HashAsync([NotNull] Stream stream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        OperationHelpers.ThrowIfNotReadable(stream, $"Stream '{nameof(stream)}' must be readable.");
        using var sha256 = SHA256.Create();
#if NET6_0_OR_GREATER
        return await sha256.ComputeHashAsync(stream, ct).ConfigureAwait(false);
#else
        // .NET Standard 2.0: ComputeHashAsync doesn't exist, use synchronous version
        await System.Threading.Tasks.Task.Yield();
        ct.ThrowIfCancellationRequested();
        return sha256.ComputeHash(stream);
#endif
    }

    /// <summary>Computes SHA256 hash of a file asynchronously.</summary>
    [return: NotNull]
    public static async Task<byte[]> HashAsync([NotNull] string path, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(path);
#if NET6_0_OR_GREATER
        await using var stream = File.OpenRead(path);
#else
        using var stream = File.OpenRead(path);
#endif
        return await HashAsync(stream, ct).ConfigureAwait(false);
    }

    /// <summary>Extracts the property name from a LINQ expression<br />Usage: GetPropertyName(x => x.FirstName) returns "FirstName"</summary>
    [return: NotNull]
    public static string GetPropertyName<T, TProperty>([NotNull] Expression<Func<T, TProperty>> expression)
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

    /// <summary>Gets the full property path for nested properties<br />Usage: GetPropertyPath(x => x.Address.Street) returns "Address.Street" </summary>
    [return: NotNull]
    public static string GetPropertyPath<T, TProperty>([NotNull] Expression<Func<T, TProperty>> expression)
    {
        var path = string.Empty;
        var currentExpression = expression.Body;
        while (currentExpression is MemberExpression memberExpression) {
            path = memberExpression.Member.Name + (string.IsNullOrEmpty(path) ? "" : "." + path);
            currentExpression = memberExpression.Expression;
        }

        ArgumentHelpers.ThrowIf(string.IsNullOrEmpty(path), "Invalid expression. Must be a property access expression.");
        return path;
    }
}