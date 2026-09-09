using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Lyo.Common.Core.Identifiers;

/// <summary>
/// Thread-safe incrementing IDs for int, long, uint, and ulong. Each step takes a single lock; an unsupported generic argument throws at construction.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AutoIncrementIdGenerator<T>
    where T : struct
{
    private readonly IncrementRefDelegate _incrementRef;

    private readonly object _lock = new();
    private T _current;

    /// <summary>Shared generator for this <typeparamref name="T" />.</summary>
    public static AutoIncrementIdGenerator<T> Shared { get; } = new();

    /// <summary>Value last stored (not yet incremented).</summary>
    public T Current {
        get {
            lock (_lock)
                return _current;
        }
    }

    /// <summary>Starts at <paramref name="start" />. The first <see cref="Next" /> returns start + 1.</summary>
    public AutoIncrementIdGenerator(T start = default)
    {
        _incrementRef = ResolveIncrementStrategy();
        _current = start;
    }

    /// <summary>Replaces the stored value used by the next increment.</summary>
    public void SetCurrent(T value)
    {
        lock (_lock)
            _current = value;
    }

    /// <summary>Adds one and returns the new value. Throws if the add overflows.</summary>
    public T Next()
    {
        lock (_lock) {
            _incrementRef(ref _current);
            return _current;
        }
    }

    public override string ToString() => $"Current={Current}";

    private static IncrementRefDelegate ResolveIncrementStrategy()
    {
        if (typeof(T) == typeof(int))
            return IncrementInt;

        if (typeof(T) == typeof(long))
            return IncrementLong;

        if (typeof(T) == typeof(uint))
            return IncrementUInt;

        if (typeof(T) == typeof(ulong))
            return IncrementULong;

        throw new NotSupportedException($"Type {typeof(T).FullName} is not supported. Use int, long, uint, or ulong.");
    }

    private static void IncrementInt(ref T value)
    {
        checked {
            Unsafe.As<T, int>(ref value)++;
        }
    }

    private static void IncrementLong(ref T value)
    {
        checked {
            Unsafe.As<T, long>(ref value)++;
        }
    }

    private static void IncrementUInt(ref T value)
    {
        checked {
            Unsafe.As<T, uint>(ref value)++;
        }
    }

    private static void IncrementULong(ref T value)
    {
        checked {
            Unsafe.As<T, ulong>(ref value)++;
        }
    }

    private delegate void IncrementRefDelegate(ref T value);
}