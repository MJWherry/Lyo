using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Lyo.ContentThreatScan;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.ContentThreatScan.Intel;

internal sealed record ProviderAccumulator(
    List<ContentThreatContribution> Contributions,
    bool IntelConfirmedMalicious)
{
    public static ProviderAccumulator Merge(ProviderAccumulator a, ProviderAccumulator b)
    {
        var list = new List<ContentThreatContribution>(a.Contributions.Count + b.Contributions.Count);
        list.AddRange(a.Contributions);
        list.AddRange(b.Contributions);
        return new(list, a.IntelConfirmedMalicious || b.IntelConfirmedMalicious);
    }

    public ExternalReputationEnvelope Finish() =>
        new(Contributions, IntelConfirmedMalicious);
}

public sealed class DefaultContentThreatReputationPipeline : IContentThreatReputationPipeline
{
    readonly HttpClient _http;
    readonly ReputationPipelineOptions _opts;
    readonly ILogger _log;
    readonly ReputationDigestLookupCache _cache;

    public DefaultContentThreatReputationPipeline(
        HttpClient httpClient,
        ReputationPipelineOptions options,
        ILogger<DefaultContentThreatReputationPipeline>? logger = null)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _opts = options ?? throw new ArgumentNullException(nameof(options));
        _log = logger ?? NullLogger<DefaultContentThreatReputationPipeline>.Instance;
        _cache = new(Math.Max(options.DigestCacheMaximumEntries, 8));
        _http.Timeout = Timeout.InfiniteTimeSpan;
    }

    public async Task<ExternalReputationEnvelope> InspectAsync(
        ContentThreatReputationRequest request,
        ContentThreatScanContext context,
        CancellationToken ct = default)
    {
        _ = context;
        var digestBytes = request.Sha256Digest32.ToArray();
        var hexDigest = ContentThreatBuffering.Sha256DigestToHexLower(digestBytes);
        var utc = DateTime.UtcNow;
        if (_cache.TryGet(hexDigest, utc, out var cached))
            return cached;

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linked.CancelAfter(_opts.ProviderTimeout);

        var acc = EmptyAcc();
        var pending = new List<Task<ProviderAccumulator>>();

        if (!string.IsNullOrWhiteSpace(_opts.VirusTotalApiKey))
            pending.Add(SafeProbe("vt", ct, linked.Token, () => ProbeVt(hexDigest, linked.Token)));

        if (!string.IsNullOrWhiteSpace(_opts.MalwareBazaarAuthKey))
            pending.Add(SafeProbe("bazaar", ct, linked.Token, () => ProbeMb(hexDigest, linked.Token)));

        if (_opts.Clamd.Enabled && request.LimitedSamplePrefix is { Length: > 0 } probe)
            pending.Add(SafeProbe("clamd", ct, linked.Token, () => ProbeClam(probe, linked.Token)));

        if (pending.Count > 0) {
            foreach (var slice in await Task.WhenAll(pending).ConfigureAwait(false))
                acc = ProviderAccumulator.Merge(acc, slice);
        }

        var envelope = acc.Finish();
        var benign = envelope.Contributions.Count == 0 && !envelope.IntelConfirmedMalicious;
        var ttl = benign
            ? TimeSpan.FromMinutes(Math.Max(_opts.NegativeCacheMinutes, 1))
            : TimeSpan.FromMinutes(Math.Max(_opts.PositiveMalwareCacheMinutes, 5));

        _cache.Put(hexDigest, envelope, utc, ttl);
        return envelope;
    }

    static ProviderAccumulator EmptyAcc() =>
        new(new List<ContentThreatContribution>(), IntelConfirmedMalicious: false);

    async Task<ProviderAccumulator> SafeProbe(
        string name,
        CancellationToken userCt,
        CancellationToken linkedCt,
        Func<Task<ProviderAccumulator>> probe)
    {
        try {
            return await probe().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (userCt.IsCancellationRequested) {
            throw;
        }
        catch (OperationCanceledException) when (linkedCt.IsCancellationRequested && !userCt.IsCancellationRequested) {
            return FailureAcc($"reputation.{name}.timeout", FailMode(name));
        }
        catch (Exception ex) {
            _log.LogWarning(ex, "Reputation probe '{Probe}' failed.", name);
            return FailureAcc($"reputation.{name}.failure", FailMode(name));
        }
    }

    ExternalReputationFailureDisposition FailMode(string name) =>
        name switch {
            "vt" => _opts.VirusTotalFailureDisposition,
            "bazaar" => _opts.MalwareBazaarFailureDisposition,
            _ => _opts.Clamd.FailureDisposition
        };

    ProviderAccumulator FailureAcc(string ruleId, ExternalReputationFailureDisposition mode) =>
        mode switch {
            ExternalReputationFailureDisposition.TreatAsSuspect => new(
                new List<ContentThreatContribution> { new(ruleId, ContentThreatCategory.Reputation, Math.Max(_opts.ProviderFailureSuspectBump, 0m)) },
                IntelConfirmedMalicious: false),
            ExternalReputationFailureDisposition.ImmediateThreatBump => new(
                new List<ContentThreatContribution>
                    { new(ruleId + ".threat", ContentThreatCategory.Reputation, Math.Max(_opts.ProviderFailureThreatBump, 0m)) },
                IntelConfirmedMalicious: false),
            _ => EmptyAcc()
        };

    async Task<ProviderAccumulator> ProbeMb(string shaHex, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, _opts.MalwareBazaarEndpoint);
        req.Headers.TryAddWithoutValidation("Auth-Key", _opts.MalwareBazaarAuthKey!.Trim());
        req.Content =
            new StringContent(
                $"query=get_info&hash={Uri.EscapeDataString(shaHex.ToLowerInvariant())}",
                Encoding.UTF8,
                "application/x-www-form-urlencoded");

        using HttpResponseMessage resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        var txt = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        using var json = JsonDocument.Parse(txt);
        var root = json.RootElement;
        if (!root.TryGetProperty("query_status", out var qs))
            return EmptyAcc();

        var stat = (qs.GetString() ?? string.Empty).Trim().ToLowerInvariant();
        if (stat.Length == 0 || stat == "hash_not_found")
            return EmptyAcc();

        if (stat.Contains("illegal") || stat.Contains("http_post_expected"))
            return EmptyAcc();

        if (stat != "ok" && !root.TryGetProperty("sha256_hash", out _))
            return EmptyAcc();

        var list = new List<ContentThreatContribution>
        {
            new("reputation.malware_bazaar", ContentThreatCategory.Reputation, Math.Max(_opts.MalwareBazaarKnownSamplePoints, 0m))
        };

        if (root.TryGetProperty("signature", out var sigEl) && sigEl.ValueKind == JsonValueKind.String) {
            var family = sigEl.GetString();
            if (!string.IsNullOrWhiteSpace(family))
                list.Add(new("reputation.malware_bazaar.family", ContentThreatCategory.Reputation, 4m, family));
        }

        return new(list, IntelConfirmedMalicious: true);
    }

    async Task<ProviderAccumulator> ProbeVt(string shaHex, CancellationToken ct)
    {
        var url = new Uri(_opts.VirusTotalApiRoot, $"files/{shaHex.ToLowerInvariant()}");
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("x-apikey", _opts.VirusTotalApiKey!.Trim());

        using HttpResponseMessage resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        if (resp.StatusCode == HttpStatusCode.NotFound)
            return EmptyAcc();

        resp.EnsureSuccessStatusCode();

        var txt = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        using var doc = JsonDocument.Parse(txt);
        var attrs = TryAttrs(doc.RootElement);
        if (attrs.ValueKind != JsonValueKind.Object || !attrs.TryGetProperty("last_analysis_stats", out var stats)
            || stats.ValueKind != JsonValueKind.Object)
            return EmptyAcc();

        var malicious =
            stats.TryGetProperty("malicious", out var malEl) && malEl.TryGetInt32(out var malCount)
                ? malCount
                : 0;

        decimal points = malicious * Math.Max(_opts.VirusTotalPointsPerMaliciousEngine, 0m);
        List<ContentThreatContribution> contrib = new();
        if (points > 0m)
            contrib.Add(new("reputation.vt", ContentThreatCategory.Reputation, Math.Min(points, 120m)));

        var confirm =
            malicious >= Math.Max(_opts.VirusTotalMinimumMaliciousEnginesForIntelConfirmation, 1);
        return new(contrib, confirm);
    }

    static JsonElement TryAttrs(JsonElement root)
    {
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty("attributes", out var attrs))
            return default;

        return attrs.ValueKind == JsonValueKind.Object ? attrs : default;
    }

    async Task<ProviderAccumulator> ProbeClam(ReadOnlyMemory<byte> sample, CancellationToken ct)
    {
        using TcpClient tcp = new();
#if NET5_0_OR_GREATER
        await tcp.ConnectAsync(_opts.Clamd.Host, _opts.Clamd.Port, ct).ConfigureAwait(false);
#else
        await tcp.ConnectAsync(_opts.Clamd.Host, _opts.Clamd.Port).ConfigureAwait(false);
#endif

        NetworkStream ns = tcp.GetStream();
        ns.ReadTimeout = Math.Max(_opts.Clamd.TcpConnectTimeoutMilliseconds, 3000);
        ns.WriteTimeout = Math.Max(_opts.Clamd.TcpConnectTimeoutMilliseconds, 3000);

        byte[] preamble = Encoding.ASCII.GetBytes("zINSTREAM\0");

#if NETSTANDARD2_0
        await ns.WriteAsync(preamble, 0, preamble.Length, ct).ConfigureAwait(false);
#else
        await ns.WriteAsync(preamble.AsMemory(0), ct).ConfigureAwait(false);
#endif

        var leasedSample = sample.ToArray();
        var walked = 0;
        while (walked < leasedSample.Length) {
            ct.ThrowIfCancellationRequested();
            var take =
                leasedSample.Length - walked <= _opts.Clamd.InstreamChunkSize
                    ? leasedSample.Length - walked
                    : _opts.Clamd.InstreamChunkSize;
            var chunk = new byte[take];

            Buffer.BlockCopy(leasedSample, walked, chunk, 0, take);
            walked += take;
            await SendClamChunk(ns, chunk, ct).ConfigureAwait(false);
        }

        await SendClamChunk(ns, [], ct).ConfigureAwait(false);

        byte[] ascii = await ReadLineAscii(ns, ct).ConfigureAwait(false);
        string msg = ascii.Length == 0 ? "" : Encoding.ASCII.GetString(ascii).Trim();

        if (msg.StartsWith("FOUND", StringComparison.OrdinalIgnoreCase)) {
            string detail =
                msg.Length <= 260 ? msg : msg.Substring(0, 260) + "...";
            List<ContentThreatContribution> contrib =
                new()
                {
                    new("clamd.detected", ContentThreatCategory.AntiMalwareEngine, Math.Max(_opts.Clamd.EngineDetectionPoints, 0m), detail)
                };
            return new(contrib, _opts.Clamd.EngineDetectionMarksIntelConfirmed);
        }

        return EmptyAcc();
    }

    static async Task SendClamChunk(NetworkStream ns, byte[] payload, CancellationToken ct)
    {
        var len = payload.Length;
        byte[] hdr =
        {
            (byte)((uint)len >> 24),
            (byte)((uint)len >> 16),
            (byte)((uint)len >> 8),
            (byte)(uint)len
        };

#if NETSTANDARD2_0
        await ns.WriteAsync(hdr, 0, hdr.Length, ct).ConfigureAwait(false);
        if (len != 0)
            await ns.WriteAsync(payload, 0, payload.Length, ct).ConfigureAwait(false);
#else
        await ns.WriteAsync(hdr.AsMemory(0), ct).ConfigureAwait(false);
        if (len != 0)
            await ns.WriteAsync(payload.AsMemory(0), ct).ConfigureAwait(false);
#endif
    }

    static async Task<byte[]> ReadLineAscii(NetworkStream ns, CancellationToken ct)
    {
        MemoryStream mem = new();
        byte[] buffer = new byte[512];

        while (true) {
            ct.ThrowIfCancellationRequested();

#if NETSTANDARD2_0

            var readCount = await ns.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);


#else

            var readCount = await ns.ReadAsync(buffer.AsMemory(), ct).ConfigureAwait(false);



#endif




            if (readCount == 0)
                break;

            for (var i = 0; i < readCount; i++) {
                if (buffer[i] == (byte)'\n')
                    return mem.ToArray();

                mem.WriteByte(buffer[i]);
            }
        }

        return mem.ToArray();
    }


}

