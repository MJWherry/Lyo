namespace Lyo.Idempotency;

/// <summary>Decides what a live row means for a new claim. Expired rows are not passed in.</summary>
internal static class IdempotencyDecision
{
    public static IdempotencyBeginResult FromLive(IdempotencyEntry live, byte[] requestHash, DateTime utcNow)
    {
        var timeExpired = IdempotencyRules.IsTimeExpired(live.ExpiresAt, utcNow);
        if (live.State == IdempotencyState.InProgress && IdempotencyHash.Matches(live.RequestHash, requestHash))
            return Result(IdempotencyOutcome.InProgress, live);

        if (live.State == IdempotencyState.InProgress)
            return Result(IdempotencyOutcome.Conflict, live);

        if (timeExpired || !IdempotencyHash.Matches(live.RequestHash, requestHash))
            return Result(IdempotencyOutcome.Conflict, live);

        return Result(IdempotencyOutcome.Replay, live);
    }

    public static IdempotencyBeginResult Began(IdempotencyEntry entry)
        => new() {
            Outcome = IdempotencyOutcome.Began,
            Id = entry.Id,
            LeaseToken = entry.LeaseToken
        };

    private static IdempotencyBeginResult Result(IdempotencyOutcome outcome, IdempotencyEntry live)
        => new() {
            Outcome = outcome,
            Id = live.Id,
            ResponseStatus = outcome == IdempotencyOutcome.Replay ? live.ResponseStatus : null,
            ResponseBody = outcome == IdempotencyOutcome.Replay ? live.ResponseBody : null,
            ResponseContentType = outcome == IdempotencyOutcome.Replay ? live.ResponseContentType : null,
            ResponseOmitted = outcome == IdempotencyOutcome.Replay && live.ResponseOmitted
        };
}
