namespace Lyo.Common.Core.Net;

/// <summary>
/// Header names Lyo services share, so an inbound reader and an outbound writer cannot drift on casing or spelling. Use these instead of inline literals whenever the
/// header is part of the Lyo contract.
/// </summary>
/// <remarks>
/// <para>Vendor-mandated spellings (Typecast's <c>X-API-KEY</c>, Endato's <c>galaxy-ap-name</c>) stay in their vendor clients: those are third-party wire contracts,
/// not Lyo conventions, and matching this class's casing would break the vendor call.</para>
/// </remarks>
public static class LyoHttpHeaders
{
    /// <summary>Standard credential header with a scheme prefix, for example <c>Bearer {token}</c>.</summary>
    public const string Authorization = "Authorization";

    /// <summary>Lyo API key header, carrying the raw secret with no scheme prefix. Accepted as an alternative to <see cref="Authorization" /> by Lyo auth schemes.</summary>
    public const string ApiKey = "X-Api-Key";

    /// <summary>Primary correlation-id header written and read by <c>Lyo.Diagnostic</c>.</summary>
    public const string CorrelationId = "X-Correlation-Id";

    /// <summary>Secondary correlation-id header, accepted for interop with services that only emit this spelling.</summary>
    public const string RequestId = "X-Request-Id";

    /// <summary>Correlation headers in priority order: the first populated header wins on read; every entry is written on send.</summary>
    public static readonly string[] CorrelationIds = [CorrelationId, RequestId];
}
