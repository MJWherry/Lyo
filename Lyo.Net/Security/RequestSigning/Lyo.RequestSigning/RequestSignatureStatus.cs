namespace Lyo.RequestSigning;

/// <summary>Result of checking a <c>Lyo-Signature</c> header.</summary>
public enum RequestSignatureStatus
{
    /// <summary>The header matches the current key or <see cref="RequestSignatureOptions.PreviousKey" />, and the timestamp is inside the max age.</summary>
    Valid,

    /// <summary>The header is missing.</summary>
    Missing,

    /// <summary>The header is not <c>t=&lt;unix&gt;,v1=&lt;hex&gt;</c>.</summary>
    Malformed,

    /// <summary>The timestamp is outside the max age.</summary>
    Stale,

    /// <summary>The MAC does not match the current key or <see cref="RequestSignatureOptions.PreviousKey" />.</summary>
    Invalid
}