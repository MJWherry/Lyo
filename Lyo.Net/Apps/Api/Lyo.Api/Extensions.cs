using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Lyo.Api.Models;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Error;
using Lyo.Common.Core.Conversion;
using Microsoft.AspNetCore.Http;

namespace Lyo.Api;

/// <summary>
/// Cross-cutting helpers for API hosts: problem details from exceptions, uploaded-file hashing, and conversion wrappers used by validation and patch binding. Type and value
/// conversion delegates to <see cref="TypeConversion" /> in Lyo.Common.Core (with lenient boolean parsing for patch payloads).
/// </summary>
public static class Extensions
{
    /// <summary>Builds <see cref="LyoProblemDetails" /> from an exception, including trace and span identifiers when <see cref="Activity.Current" /> is set.</summary>
    /// <param name="ex">Exception to surface. Its stack trace is deliberately omitted from the response; log it separately.</param>
    /// <param name="message">Optional user-facing message. Defaults to <see cref="Exception.Message" />.</param>
    /// <param name="errorCode">Stable API error code (defaults to the unknown-error code in <see cref="Lyo.Api.Models.Constants.ApiErrorCodes" />).</param>
    public static LyoProblemDetails ApiErrorFromException(Exception ex, string? message = null, string errorCode = Constants.ApiErrorCodes.Unknown)
        => LyoProblemDetailsBuilder.CreateWithTrace(Activity.Current?.TraceId.ToString(), Activity.Current?.SpanId.ToString())
            .WithErrorCode(errorCode)
            .WithMessage(message ?? ex.Message)
            .AddApiError(errorCode, message ?? ex.Message)
            .Build();

    /// <summary>Computes the SHA-256 hash of an uploaded form file.</summary>
    /// <param name="file">Form file whose content is hashed.</param>
    /// <param name="ct">Token used to cancel the hash.</param>
    /// <returns>The 32-byte SHA-256 digest.</returns>
    public static async Task<byte[]> HashAsync(this IFormFile file, CancellationToken ct = default)
    {
        using var sha256 = SHA256.Create();
        var stream = file.OpenReadStream();
        try {
            var hashBytes = await sha256.ComputeHashAsync(stream, ct).ConfigureAwait(false);
            return hashBytes;
        }
        finally {
            await stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    extension(object? obj)
    {
        /// <summary>Converts a single value to the specified type with comprehensive type handling.</summary>
        public object? ConvertToTargetType(Type targetType)
        {
            if (obj == null) {
                return targetType.IsNullable() || !targetType.IsValueType
                    ? null
                    : throw new ArgumentNullException(nameof(obj), $"Cannot convert null to non-nullable type {targetType.Name}");
            }

            return TypeConversion.ConvertTo(obj, targetType, true);
        }

        /// <summary>Converts an object (single value or collection) to the specified type.</summary>
        public object? ConvertToType(Type targetType) => TypeConversion.ConvertToWithCollections(obj, targetType, true);
    }

    extension(JsonElement element)
    {
        /// <summary>Safely extracts a value from a JsonElement.</summary>
        public object? ExtractValueFromJsonElement() => TypeConversion.FromJsonElement(element);

        /// <summary>Extracts an array of values from a JsonElement array.</summary>
        public IEnumerable<object?> ExtractArrayFromJsonElement()
            => element.ValueKind != JsonValueKind.Array
                ? throw new ArgumentException("JsonElement is not an array", nameof(element))
                : (IEnumerable<object?>)TypeConversion.FromJsonElement(element)!;
    }
}