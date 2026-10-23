using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.RequestSigning;

/// <summary>Header name, HMAC key, and max age for <see cref="RequestSigner" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class RequestSignatureOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "RequestSigning";

    /// <summary>Default header name.</summary>
    public const string DefaultHeaderName = "Lyo-Signature";

    /// <summary>HMAC key. The host loads this from its configuration provider.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Previous HMAC key. Verify accepts it so a host can rotate by publishing both values, then dropping this one.</summary>
    public string? PreviousKey { get; set; }

    /// <summary>How far a timestamp may sit from utc now. The boundary itself is still valid. Default is 5 minutes.</summary>
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Header that carries <c>t</c> and <c>v1</c>.</summary>
    public string HeaderName { get; set; } = DefaultHeaderName;

    /// <summary>Throws when the key, header name, or max age is unusable.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(Key);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(HeaderName);
        ArgumentHelpers.ThrowIfLessThanOrEqual(MaxAge.Ticks, 0L, nameof(MaxAge));
    }

    /// <summary>Header name and max age. HMAC key values are omitted.</summary>
    public override string ToString() => $"HeaderName={HeaderName} MaxAge={MaxAge} KeySet={!string.IsNullOrWhiteSpace(Key)} PreviousKeySet={!string.IsNullOrWhiteSpace(PreviousKey)}";
}