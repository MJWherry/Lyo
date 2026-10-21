using Lyo.Common.Core.Enums;
using Lyo.Exceptions;
using Lyo.Hashing;

namespace Lyo.RequestSigning;

/// <summary>Signs with <see cref="RequestSignatureOptions.Key" /> and checks the header against that key and <see cref="RequestSignatureOptions.PreviousKey" />.</summary>
public sealed class RequestSigner
{
    private readonly IHashingService _hashing;
    private readonly RequestSignatureOptions _options;
    private readonly Func<DateTime> _utcNow;

    /// <summary>Header name from options.</summary>
    public string HeaderName => _options.HeaderName;

    /// <summary>Builds a signer. <paramref name="options" /> is validated here.</summary>
    public RequestSigner(IHashingService hashing, RequestSignatureOptions options, Func<DateTime>? utcNow = null)
    {
        ArgumentHelpers.ThrowIfNull(hashing);
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        _hashing = hashing;
        _options = options;
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    /// <summary>Writes a header for <paramref name="input" /> using <see cref="RequestSignatureOptions.Key" /> and the current unix time.</summary>
    public string CreateHeader(RequestSignatureInput input)
    {
        ArgumentHelpers.ThrowIfNull(input);
        var unix = Unix(Utc());
        var mac = RequestSignature.Mac(_options.Key, input, unix, _hashing);
        return RequestSignature.FormatHeader(unix, _hashing.ToHex(mac, TextLetterCase.Lower));
    }

    /// <summary>Checks <paramref name="header" />. The current key and <see cref="RequestSignatureOptions.PreviousKey" /> are both compared when the previous key is set.</summary>
    public RequestSignatureStatus Verify(RequestSignatureInput input, string? header)
    {
        ArgumentHelpers.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(header))
            return RequestSignatureStatus.Missing;

        if (!RequestSignature.TryParse(header, out var unix, out var macHex))
            return RequestSignatureStatus.Malformed;

        byte[] presented;
        try {
            presented = _hashing.ParseHex(macHex);
        }
        catch (FormatException) {
            return RequestSignatureStatus.Malformed;
        }

        var nowUnix = Unix(Utc());
        var maxSeconds = (long)_options.MaxAge.TotalSeconds;
        if (Math.Abs(nowUnix - unix) > maxSeconds)
            return RequestSignatureStatus.Stale;

        var matched = Matches(_options.Key, input, unix, presented);
        if (!string.IsNullOrEmpty(_options.PreviousKey))
            matched |= Matches(_options.PreviousKey, input, unix, presented);

        return matched ? RequestSignatureStatus.Valid : RequestSignatureStatus.Invalid;
    }

    private bool Matches(string key, RequestSignatureInput input, long unix, byte[] presented)
    {
        var expected = RequestSignature.Mac(key, input, unix, _hashing);
        return _hashing.FixedTimeEquals(expected, presented);
    }

    private DateTime Utc()
    {
        var now = _utcNow();
        if (now.Kind == DateTimeKind.Local)
            throw new InvalidOperationException("The request signer clock must return UTC.");

        return now.Kind == DateTimeKind.Utc ? now : DateTime.SpecifyKind(now, DateTimeKind.Utc);
    }

    private static long Unix(DateTime utc) => new DateTimeOffset(utc).ToUnixTimeSeconds();
}