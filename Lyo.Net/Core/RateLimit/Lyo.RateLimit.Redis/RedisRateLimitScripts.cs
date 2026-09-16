namespace Lyo.RateLimit.Redis;

/// <summary>One atomic script per window. A negative remaining means the call was rejected with no write.</summary>
internal static class RedisRateLimitScripts
{
    internal const string Fixed = """
        local now = tonumber(ARGV[1])
        if now == nil or now == 0 then
          local t = redis.call('TIME')
          now = tonumber(t[1]) * 1000 + math.floor(tonumber(t[2]) / 1000)
        end
        local limit = tonumber(ARGV[2])
        local window = tonumber(ARGV[3])
        local stored = redis.call('HGET', KEYS[1], 'limit')
        if stored and tonumber(stored) ~= limit then
          return {0, now, -1, 0, 0}
        end
        local windowId = math.floor(now / window)
        local prev = redis.call('HGET', KEYS[1], 'wid')
        if prev and tonumber(prev) > windowId then
          return {0, now, -1, 0, 0}
        end
        local count = 1
        if prev and tonumber(prev) == windowId then
          count = tonumber(redis.call('HGET', KEYS[1], 'count')) + 1
        end
        redis.call('HSET', KEYS[1], 'limit', limit, 'wid', windowId, 'count', count)
        local retry = window - (now % window)
        if retry <= 0 then retry = window end
        redis.call('PEXPIRE', KEYS[1], retry + window)
        local allowed = 0
        if count <= limit then allowed = 1 end
        local remaining = limit - count
        if remaining < 0 then remaining = 0 end
        return {allowed, now, remaining, 0, 0}
        """;

    internal const string Sliding = """
        local now = tonumber(ARGV[1])
        if now == nil or now == 0 then
          local t = redis.call('TIME')
          now = tonumber(t[1]) * 1000 + math.floor(tonumber(t[2]) / 1000)
        end
        local limit = tonumber(ARGV[2])
        local window = tonumber(ARGV[3])
        local stored = redis.call('HGET', KEYS[1], 'limit')
        if stored and tonumber(stored) ~= limit then
          return {0, now, -1, 0, 0}
        end
        local elapsed = now % window
        local windowId = math.floor(now / window)
        local prevIdRaw = redis.call('HGET', KEYS[1], 'wid')
        local previous = 0
        local current = 0
        if prevIdRaw then
          local prevId = tonumber(prevIdRaw)
          if windowId < prevId then
            return {0, now, -1, 0, 0}
          end
          local prevRaw = redis.call('HGET', KEYS[1], 'prev')
          local curRaw = redis.call('HGET', KEYS[1], 'cur')
          if prevRaw then previous = tonumber(prevRaw) end
          if curRaw then current = tonumber(curRaw) end
          if windowId == prevId + 1 then
            previous = current
            current = 0
          elseif windowId > prevId + 1 then
            previous = 0
            current = 0
          end
        end
        local function used_milli(p, e, w)
          if p <= 0 then return 0 end
          local remain = w - e
          if remain <= 0 then return 0 end
          return math.floor((p * remain * 1000 + w - 1) / w)
        end
        local used = used_milli(previous, elapsed, window) + current * 1000
        local allowed = 0
        if used + 1000 <= limit * 1000 then
          allowed = 1
          current = current + 1
          used = used + 1000
        end
        redis.call('HSET', KEYS[1], 'limit', limit, 'wid', windowId, 'prev', previous, 'cur', current)
        local ttl = (2 * window) - elapsed
        if ttl < 1 then ttl = 1 end
        redis.call('PEXPIRE', KEYS[1], ttl)
        local remaining = math.floor((limit * 1000 - used) / 1000)
        if remaining < 0 then remaining = 0 end
        if allowed == 0 then
          return {0, now, remaining, previous, current}
        end
        return {1, now, remaining, previous, current}
        """;

    internal const string TokenBucket = """
        local now = tonumber(ARGV[1])
        if now == nil or now == 0 then
          local t = redis.call('TIME')
          now = tonumber(t[1]) * 1000 + math.floor(tonumber(t[2]) / 1000)
        end
        local limit = tonumber(ARGV[2])
        local window = tonumber(ARGV[3])
        local stored = redis.call('HGET', KEYS[1], 'limit')
        if stored and tonumber(stored) ~= limit then
          return {0, now, -1, 0, 0}
        end
        local full = limit * 1000
        local tokens = redis.call('HGET', KEYS[1], 'tokens')
        local ts = redis.call('HGET', KEYS[1], 'ts')
        if (not tokens) or (not ts) then
          tokens = full
          ts = now
        else
          tokens = tonumber(tokens)
          ts = tonumber(ts)
          local elapsed = now - ts
          if elapsed < 0 then elapsed = 0 end
          if elapsed > window then elapsed = window end
          local add = math.floor((elapsed * limit * 1000) / window)
          if add > 0 then
            tokens = tokens + add
            if tokens > full then tokens = full end
            ts = now
          end
        end
        local allowed = 0
        if tokens >= 1000 then
          allowed = 1
          tokens = tokens - 1000
        end
        redis.call('HSET', KEYS[1], 'limit', limit, 'tokens', tokens, 'ts', ts)
        redis.call('PEXPIRE', KEYS[1], window * 2)
        local remaining = math.floor(tokens / 1000)
        if remaining < 0 then remaining = 0 end
        return {allowed, now, remaining, tokens, 0}
        """;
}
