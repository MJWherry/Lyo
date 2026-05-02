namespace Lyo.Diagnostic.Breadcrumbs;

/// <summary>Thread-safe bounded breadcrumb trail using a FIFO queue; oldest entries drop when over capacity.</summary>
public sealed class RingBufferBreadcrumbTrail : IBreadcrumbTrail
{
    private readonly object _lock = new();
    private readonly Queue<Breadcrumb> _queue = new();
    private readonly int _capacity;
    private readonly IBreadcrumbRedactor _redactor;

    /// <param name="capacity">Maximum entries retained; must be at least 1.</param>
    /// <param name="redactor">Applied on every add; defaults to <see cref="PassThroughBreadcrumbRedactor.Instance" />.</param>
    public RingBufferBreadcrumbTrail(int capacity, IBreadcrumbRedactor? redactor = null)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
        _redactor = redactor ?? PassThroughBreadcrumbRedactor.Instance;
    }

    /// <inheritdoc />
    public void Add(Breadcrumb breadcrumb)
    {
        var safe = _redactor.Redact(breadcrumb);
        lock (_lock) {
            while (_queue.Count >= _capacity)
                _queue.Dequeue();
            _queue.Enqueue(safe);
        }
    }

    /// <inheritdoc />
    public void Add(string category, string message, IReadOnlyDictionary<string, string>? data = null)
        => Add(new Breadcrumb(DateTimeOffset.UtcNow, category, message, data));

    /// <inheritdoc />
    public IReadOnlyList<Breadcrumb> Snapshot()
    {
        lock (_lock)
            return _queue.ToArray();
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock)
            _queue.Clear();
    }
}
