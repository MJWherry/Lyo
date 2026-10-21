namespace Lyo.Xlsx;

/// <summary>Holds one cloned options instance for the current call. A nested call keeps the outer clone. There is no lock.</summary>
internal sealed class CallSnapshot<T>
    where T : class
{
    private readonly Func<T, T> _clone;
    private readonly AsyncLocal<T?> _current = new();
    private readonly Func<T> _read;

    public T Current => _current.Value ?? _clone(_read());

    public CallSnapshot(Func<T> read, Func<T, T> clone)
    {
        _read = read;
        _clone = clone;
    }

    public IDisposable Enter()
    {
        if (_current.Value is not null)
            return Empty.Instance;

        _current.Value = _clone(_read());
        return new Scope(this);
    }

    private void Clear() => _current.Value = null;

    private sealed class Scope(CallSnapshot<T> owner) : IDisposable
    {
        public void Dispose() => owner.Clear();
    }

    private sealed class Empty : IDisposable
    {
        public static readonly Empty Instance = new();

        public void Dispose() { }
    }
}