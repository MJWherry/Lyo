using System.Diagnostics.CodeAnalysis;
using Lyo.Common.Identifiers;
using Lyo.Exceptions;

namespace Lyo.Config;

/// <summary>Maps URL segments under <c>/api/config/{appKind}/{appId}</c> to <see cref="EntityRef" /> with <see cref="AppEntityType" /> and compound id <c>{kind}:{id}</c>.</summary>
public static class AppConfigEntity
{
    /// <summary>Stored <see cref="EntityRef.EntityType" /> for definitions/bindings serviced by Config API routes.</summary>
    public const string AppEntityType = "App";

    /// <summary>Builds the config-store entity reference from URL segments.</summary>
    public static EntityRef ToEntityRef(string appKind, string appId)
    {
        if (!TryCreate(appKind, appId, out var refs, out var err))
            throw new ArgumentException(err);

        return refs;
    }

    /// <summary>Validates and maps route segments without throwing.</summary>
    public static bool TryCreate(string appKind, string appId, out EntityRef entityRef, [NotNullWhen(false)] out string? errorMessage)
    {
        entityRef = default;
        errorMessage = null;
        try {
            var kindNorm = NormalizeSegment(Uri.UnescapeDataString(appKind.Trim()), nameof(appKind));
            var rawId = appId.Trim();
            var decoded = Uri.UnescapeDataString(rawId);
            var idNorm = NormalizeSegment(decoded, nameof(appId));
            entityRef = new(AppEntityType, $"{kindNorm}:{idNorm}");
            return true;
        }
        catch (ArgumentException ex) {
            errorMessage = ex.Message;
            return false;
        }
    }

    private static string NormalizeSegment(string raw, string paramName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(raw,paramName);
        var s = raw.Trim().ToLowerInvariant();
        ValidateSlug(s, paramName);
        return s;
    }

    private static void ValidateSlug(string value, string paramName)
    {
        ArgumentHelpers.ThrowIfNotInRange(value.Length,0, 128, paramName);
        foreach (var c in value.AsSpan()) {
            var okLower = c >= 'a' && c <= 'z';
            var okDigit = c >= '0' && c <= '9';
            var ok = okLower || okDigit || c is '-' or '_' or '.';
            ArgumentHelpers.ThrowIf(!ok, "Only lowercase letters, digits, '-', '_', and '.' allowed.", paramName);
        }
    }
}