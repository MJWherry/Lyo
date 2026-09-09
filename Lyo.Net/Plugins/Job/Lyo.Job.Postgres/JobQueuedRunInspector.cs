using System.Text;
using System.Text.Json;
using Lyo.Job.Models.Events;
using Lyo.MessageQueue;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Constants = Lyo.Job.Models.Constants;
using Lyo.Exceptions;

namespace Lyo.Job.Postgres;

/// <summary>
/// Reads the worker dispatch queues to find which runs already have a message waiting. Resync uses this to republish only the runs the broker actually lost. Republishing blindly
/// would hand every worker a duplicate for runs that were merely slow to be picked up. Peek failures are treated as "queue empty", so a broker hiccup can cause a duplicate
/// dispatch but never a missed one. The run lifecycle is idempotent on the worker side (<c>StartedJobRun</c> transitions <c>Queued</c> to <c>Running</c> once).
/// </summary>
/// <param name="mqService">Transport to peek, or null when the host has none.</param>
/// <param name="logger">Optional logger for peek failures.</param>
public sealed class JobQueuedRunInspector(IMqService? mqService, ILogger? logger = null)
{
    /// <summary>Cap on how many messages a single peek requests, so a huge backlog cannot pull an unbounded batch out of the broker.</summary>
    private const int QueuePeekCap = 10_000;

    private readonly ILogger _logger = logger ?? NullLogger.Instance;

    /// <summary>Run ids that already have a dispatch message on the run or wait queue of any worker type in <paramref name="workerTypes" />.</summary>
    /// <param name="workerTypes">Worker types to inspect. Duplicates are ignored.</param>
    /// <param name="expectedRunCount">How many runs the caller is considering, used to size the peek.</param>
    /// <param name="ct">Token used to cancel the peek.</param>
    public async Task<HashSet<Guid>> GetQueuedRunIdsAsync(IEnumerable<string> workerTypes, int expectedRunCount, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(workerTypes);
        var ids = new HashSet<Guid>();
        if (mqService is null || !mqService.IsConnected())
            return ids;

        var peekCount = Math.Clamp(expectedRunCount, 1, QueuePeekCap);
        foreach (var workerType in workerTypes.Distinct(StringComparer.Ordinal)) {
            await AddPeekedRunIdsAsync(ids, Constants.Mq.QueueGetJobRunCreated(workerType), peekCount, ct).ConfigureAwait(false);
            await AddPeekedRunIdsAsync(ids, Constants.Mq.QueueGetJobRunCreatedWait(workerType), peekCount, ct).ConfigureAwait(false);
        }

        return ids;
    }

    /// <summary>
    /// Extracts the run id from a peeked message. Dispatch payloads have appeared in three shapes over time: a bare GUID, a <see cref="QueueMessageEnvelope{T}" />, and an object
    /// with a <c>Payload</c> property. A queue can hold all of them during a rolling deploy, so each is tried in turn.
    /// </summary>
    /// <param name="message">Peeked message.</param>
    /// <param name="runId">Parsed run id when this returns true.</param>
    public static bool TryParseQueuedRunId(QueuePeekMessage message, out Guid runId)
    {
        ArgumentHelpers.ThrowIfNull(message);
        runId = default;
        var payload = message.Payload;
        if (string.IsNullOrWhiteSpace(payload))
            return false;

        if (string.Equals(message.PayloadEncoding, "base64", StringComparison.OrdinalIgnoreCase)) {
            try {
                payload = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            }
            catch (FormatException) {
                return false;
            }
        }

        if (Guid.TryParse(payload.Trim('"'), out runId))
            return true;

        try {
            var envelope = JsonSerializer.Deserialize<QueueMessageEnvelope<Guid>>(payload);
            if (envelope is not null && envelope.Payload != Guid.Empty) {
                runId = envelope.Payload;
                return true;
            }
        }
        catch (JsonException) {
            /* Fall through to Payload-property parse. */
        }

        try {
            using var doc = JsonDocument.Parse(payload);
            if (!TryGetPayloadElement(doc.RootElement, out var payloadElement))
                return false;

            return payloadElement.ValueKind == JsonValueKind.String ? Guid.TryParse(payloadElement.GetString(), out runId) : payloadElement.TryGetGuid(out runId);
        }
        catch (JsonException) {
            return false;
        }
    }

    private async Task AddPeekedRunIdsAsync(HashSet<Guid> ids, string queueName, int peekCount, CancellationToken ct)
    {
        IReadOnlyList<QueuePeekMessage> messages;
        try {
            messages = await mqService!.PeekQueueMessages(queueName, peekCount, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to peek queue {QueueName} during queued-run resync; treating as empty", queueName);
            return;
        }

        foreach (var message in messages) {
            if (TryParseQueuedRunId(message, out var runId))
                ids.Add(runId);
        }
    }

    private static bool TryGetPayloadElement(JsonElement root, out JsonElement payload)
    {
        if (root.TryGetProperty("Payload", out payload) || root.TryGetProperty("payload", out payload))
            return true;

        payload = default;
        return false;
    }
}
