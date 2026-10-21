namespace Lyo.RateLimit;

/// <summary>Integer quota math shared by the in-memory store and the Redis result mapping. Scale is 1000 milli-permits.</summary>
internal static class RateLimitMath
{
    internal static long FixedRetryAfterMs(long nowMs, long windowMs)
    {
        var mod = nowMs % windowMs;
        if (mod < 0)
            mod += windowMs;

        var retry = windowMs - mod;
        return retry == 0 ? windowMs : retry;
    }

    internal static long SlidingUsedMilli(long previous, long elapsedMs, long windowMs)
    {
        if (previous <= 0 || windowMs <= 0)
            return 0;

        var remain = windowMs - elapsedMs;
        if (remain <= 0)
            return 0;

        var numerator = previous * remain * 1000L;
        return (numerator + windowMs - 1) / windowMs;
    }

    internal static bool SlidingAllows(long previous, long current, long elapsedMs, long windowMs, int permitLimit)
    {
        var used = SlidingUsedMilli(previous, elapsedMs, windowMs) + current * 1000L;
        return used + 1000L <= permitLimit * 1000L;
    }

    internal static long SlidingRetryAfterMs(long previous, long current, long elapsedMs, long windowMs, int permitLimit)
    {
        if (SlidingAllows(previous, current, elapsedMs, windowMs, permitLimit))
            return 0;

        var untilBoundary = windowMs - elapsedMs;
        if (untilBoundary <= 0)
            untilBoundary = windowMs;

        if (current + 1 <= permitLimit) {
            var at = EarliestSlidingElapsed(previous, current, windowMs, permitLimit);
            if (at >= elapsedMs)
                return at - elapsedMs;
        }

        if (SlidingAllows(current, 0, 0, windowMs, permitLimit))
            return untilBoundary;

        var nextElapsed = EarliestSlidingElapsed(current, 0, windowMs, permitLimit);
        if (nextElapsed < 0)
            return untilBoundary + windowMs;

        return untilBoundary + nextElapsed;
    }

    internal static long TokenRetryAfterMs(long tokensMilli, int permitLimit, long windowMs)
    {
        if (tokensMilli >= 1000)
            return 0;

        var need = 1000 - tokensMilli;
        var denom = permitLimit * 1000L;
        return (need * windowMs + denom - 1) / denom;
    }

    /// <summary>Adds refill for the elapsed time since <paramref name="timestampMs" />, capped at the window. Returns whether <paramref name="tokensMilli" /> changed.</summary>
    internal static bool TryRefillToken(ref long tokensMilli, ref long timestampMs, long nowMs, int permitLimit, long windowMs)
    {
        var elapsed = nowMs - timestampMs;
        if (elapsed < 0)
            elapsed = 0;

        if (elapsed > windowMs)
            elapsed = windowMs;

        var add = elapsed * permitLimit * 1000L / windowMs;
        if (add <= 0)
            return false;

        var full = permitLimit * 1000L;
        var sum = tokensMilli + add;
        tokensMilli = sum > full ? full : sum;
        timestampMs = nowMs;
        return true;
    }

    private static long EarliestSlidingElapsed(long previous, long current, long windowMs, int permitLimit)
    {
        if (current + 1 > permitLimit)
            return -1;

        if (SlidingAllows(previous, current, 0, windowMs, permitLimit))
            return 0;

        if (windowMs <= 1 || !SlidingAllows(previous, current, windowMs - 1, windowMs, permitLimit))
            return -1;

        long lo = 0;
        var hi = windowMs - 1;
        while (lo < hi) {
            var mid = lo + (hi - lo) / 2;
            if (SlidingAllows(previous, current, mid, windowMs, permitLimit))
                hi = mid;
            else
                lo = mid + 1;
        }

        return lo;
    }
}