using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Lyo.Common.Identifiers;

/// <summary>
/// Thread-safe auto-incrementing ID generator supporting int, long, uint, and ulong.
/// The value is incremented under a single lock; unsupported generic types throw at construction.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AutoIncrementIdGenerator<T> where T : struct
{
    private delegate void IncrementRefDelegate(ref T value);

    private readonly object _lock = new();
    private readonly IncrementRefDelegate _incrementRef;
    private T _current;

    /// <summary>A shared generator instance for the generic type argument.</summary>
    public static AutoIncrementIdGenerator<T> Shared { get; } = new();

    /// <summary>Creates a generator with an optional starting value. The first <see cref="Next"/> call returns start + 1.</summary>
    public AutoIncrementIdGenerator(T start = default)
    {
        _incrementRef = ResolveIncrementStrategy();
        _current = start;
    }

    /// <summary>Returns the current value.</summary>
    public T Current
    {
        get {
            lock (_lock)
                return _current;
        }
    }

    /// <summary>Sets the current value used by the next increment.</summary>
    public void SetCurrent(T value)
    {
        lock (_lock)
            _current = value;
    }

    /// <summary>Increments and returns the next value. Throws on numeric overflow.</summary>
    public T Next()
    {
        lock (_lock) {
            _incrementRef(ref _current);
            return _current;
        }
    }

    public override string ToString() =>  $"Current={Current}";

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
}
