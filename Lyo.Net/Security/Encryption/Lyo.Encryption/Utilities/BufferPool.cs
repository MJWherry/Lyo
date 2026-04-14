using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Lyo.Encryption.Utilities;

/// <summary>Thread-safe buffer pool for reducing memory allocations and GC pressure. Provides reusable byte arrays of various sizes to minimize allocations in hot paths.</summary>
internal static class BufferPool
{
    // Maximum number of buffers to keep per size
    private const int MaxBuffersPerSize = 32;

    // Pool sizes: 1KB, 4KB, 16KB, 64KB, 256KB, 1MB, 4MB
    private static readonly int[] PoolSizes = [1024, 4096, 16384, 65536, 262144, 1048576, 4194304];

    // Thread-safe pools for each size
    private static readonly ConcurrentQueue<byte[]>[] Pools = new ConcurrentQueue<byte[]>[PoolSizes.Length];

    static BufferPool()
    {
        for (var i = 0; i < Pools.Length; i++)
            Pools[i] = new();
    }

    /// <summary>Rents a buffer of at least the specified size. The buffer may be larger than requested.</summary>
    /// <param name="minimumSize">Minimum size of the buffer in bytes</param>
    /// <returns>A buffer that is at least minimumSize bytes</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Rent(int minimumSize)
    {
        if (minimumSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(minimumSize), "Minimum size must be greater than 0");

        // Find the smallest pool size that can accommodate the request
        for (var i = 0; i < PoolSizes.Length; i++) {
            if (PoolSizes[i] >= minimumSize) {
                // Try to get a buffer from the pool
                if (Pools[i].TryDequeue(out var buffer))
                    return buffer;

                // Pool is empty, allocate a new buffer
                return new byte[PoolSizes[i]];
            }
        }

        // Requested size is larger than our largest pool size, allocate directly
        return new byte[minimumSize];
    }

    /// <summary>Returns a buffer to the pool for reuse. The buffer will be reused if it matches one of our pool sizes and the pool isn't full.</summary>
    /// <param name="buffer">The buffer to return to the pool</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(byte[] buffer)
    {
        if (buffer == null)
            return;

        // Find the matching pool size
        for (var i = 0; i < PoolSizes.Length; i++) {
            if (buffer.Length != PoolSizes[i])
                continue;

            // Only return if pool isn't full (to prevent unbounded growth)
            if (Pools[i].Count < MaxBuffersPerSize) {
                // Clear the buffer for security (optional but recommended for encryption)
                // Note: We skip clearing for performance - the next user will overwrite it anyway
                Pools[i].Enqueue(buffer);
            }

            return;
        }

        // Buffer doesn't match any pool size, let GC handle it
    }

    /// <summary>Gets a buffer of exactly the specified size, or the next larger pool size. This is useful when you need a buffer of a specific size.</summary>
    /// <param name="size">Exact or minimum size needed</param>
    /// <param name="exactSize">If true, returns a buffer of exactly the requested size (may allocate new). If false, returns the next larger pool size.</param>
    /// <returns>A buffer of the requested size or larger</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] RentExact(int size, bool exactSize = false)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than 0");

        if (!exactSize)
            return Rent(size);

        // Check if we have an exact match in pools
        for (var i = 0; i < PoolSizes.Length; i++) {
            if (PoolSizes[i] == size)
                return Pools[i].TryDequeue(out var buffer) ? buffer : new byte[size];
        }

        // No exact match, allocate new
        return new byte[size];

        // Use the standard Rent which gets the next larger size
    }

    /// <summary>Clears all buffers from all pools. Useful for testing or memory cleanup.</summary>
    public static void Clear()
    {
        foreach (var t in Pools) {
            while (t.TryDequeue(out var _)) {
                // Drain the queue
            }
        }
    }

    /// <summary>Gets statistics about the buffer pool usage.</summary>
    public static BufferPoolStats GetStats()
    {
        var stats = new BufferPoolStats();
        for (var i = 0; i < Pools.Length; i++)
            stats.AddPool(PoolSizes[i], Pools[i].Count);

        return stats;
    }
}

/// <summary>Statistics about buffer pool usage.</summary>
internal sealed class BufferPoolStats
{
    private readonly List<(int size, int count)> _pools = new();

    public int TotalBuffers => _pools.Sum(p => p.count);

    public int TotalMemory => _pools.Sum(p => p.size * p.count);

    public IReadOnlyList<(int size, int count)> Pools => _pools;

    internal void AddPool(int size, int count) => _pools.Add((size, count));
}