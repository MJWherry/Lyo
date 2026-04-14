using System.Collections.Concurrent;
using Lyo.Web.Components.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Lyo.Gateway.Stores;

public class HybridUserStore(ILogger<HybridUserStore> logger, IMemoryCache cache) : IUserStore
{
    private readonly ConcurrentBag<string> _tokenIds = [];

    public void AddOrUpdateUser(string tokenId, BlazorUserInfo userInfo)
    {
        userInfo.LastActivity = DateTime.UtcNow;
        var cacheKey = $"user_{tokenId}";
        var cacheOptions = new MemoryCacheEntryOptions { AbsoluteExpiration = userInfo.JwtExpiration, Priority = CacheItemPriority.High };
        cacheOptions.RegisterPostEvictionCallback(PostEvictionDelegate);
        cache.Set(cacheKey, userInfo, cacheOptions);
        if (!_tokenIds.Contains(tokenId))
            _tokenIds.Add(tokenId);

        logger.LogDebug("Added/Updated user {TokenId} ({Email}) with JWT expiration {Expiration}", tokenId, userInfo.Email, userInfo.JwtExpiration);
    }

    public BlazorUserInfo? GetUser(string tokenId)
    {
        var cacheKey = $"user_{tokenId}";
        if (cache.TryGetValue(cacheKey, out BlazorUserInfo? user)) {
            user.LastActivity = DateTime.UtcNow;
            var cacheOptions = new MemoryCacheEntryOptions { AbsoluteExpiration = user.JwtExpiration, Priority = CacheItemPriority.High };
            cacheOptions.RegisterPostEvictionCallback(PostEvictionDelegate);
            cache.Set(cacheKey, user, cacheOptions);
            return user;
        }

        logger.LogDebug("User {TokenId} not found or expired", tokenId);
        return null;
    }

    public void RemoveUser(string tokenId)
    {
        var cacheKey = $"user_{tokenId}";
        if (cache.TryGetValue(cacheKey, out BlazorUserInfo? user))
            logger.LogInformation("Manually removing user {TokenId} ({Email})", tokenId, user.Email);

        cache.Remove(cacheKey);
    }

    public IEnumerable<BlazorUserInfo> GetAllUsers()
    {
        var activeUsers = new List<BlazorUserInfo>();
        var validTokenIds = new List<string>();
        foreach (var tokenId in _tokenIds) {
            var cacheKey = $"user_{tokenId}";
            if (!cache.TryGetValue(cacheKey, out BlazorUserInfo? user))
                continue;

            activeUsers.Add(user);
            validTokenIds.Add(tokenId);
        }

        _tokenIds.Clear();
        foreach (var validTokenId in validTokenIds)
            _tokenIds.Add(validTokenId);

        logger.LogDebug("Retrieved {ActiveCount} active users", activeUsers.Count);
        return activeUsers;
    }

    public bool IsUserSignedIn(string tokenId)
    {
        var cacheKey = $"user_{tokenId}";
        return cache.TryGetValue(cacheKey, out var _);
    }

    public void UpdateUserCurrentPage(string tokenId, string currentPage)
    {
        var cacheKey = $"user_{tokenId}";
        if (cache.TryGetValue(cacheKey, out BlazorUserInfo? user)) {
            user.CurrentPage = currentPage;
            user.LastActivity = DateTime.UtcNow;
            var cacheOptions = new MemoryCacheEntryOptions { AbsoluteExpiration = user.JwtExpiration, Priority = CacheItemPriority.High };
            cacheOptions.RegisterPostEvictionCallback(PostEvictionDelegate);
            cache.Set(cacheKey, user, cacheOptions);
            logger.LogDebug("Updated current page for user {TokenId} to {CurrentPage}", tokenId, currentPage);
        }
        else
            logger.LogDebug("Attempted to update page for expired or non-existent user {TokenId}", tokenId);
    }

    public int CleanupExpiredUsers()
    {
        var activeTokenIds = new List<string>();
        var expiredCount = 0;
        foreach (var tokenId in _tokenIds) {
            var cacheKey = $"user_{tokenId}";
            if (cache.TryGetValue(cacheKey, out var _))
                activeTokenIds.Add(tokenId);
            else
                expiredCount++;
        }

        _tokenIds.Clear();
        foreach (var activeTokenId in activeTokenIds)
            _tokenIds.Add(activeTokenId);

        if (expiredCount > 0)
            logger.LogInformation("Cleaned up {ExpiredCount} expired user token IDs from bag", expiredCount);

        return expiredCount;
    }

    private void PostEvictionDelegate(object key, object? value, EvictionReason reason, object? state)
    {
        if (reason == EvictionReason.Replaced)
            return;

        var cacheKey = key.ToString();
        var tokenId = cacheKey?.Replace("user_", "");
        if (value is BlazorUserInfo removedUser && tokenId != null)
            logger.LogInformation("Auto-evicted user {TokenId} ({Email}) due to JWT expiration. Reason: {Reason}", tokenId, removedUser.Email, reason);
    }
}