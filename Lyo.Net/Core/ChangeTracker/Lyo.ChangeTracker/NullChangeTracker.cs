using Lyo.Common;

namespace Lyo.ChangeTracker;

/// <summary>No-op implementation of IChangeTracker.</summary>
public sealed class NullChangeTracker : IChangeTracker
{
    /// <summary>Gets the singleton instance.</summary>
    public static NullChangeTracker Instance { get; } = new();

    private NullChangeTracker() { }

    /// <inheritdoc />
    public void RecordChange(ChangeRecord change) { }

    /// <inheritdoc />
    public Task RecordChangeAsync(ChangeRecord change, CancellationToken ct = default) => Task.CompletedTask;

    /// <inheritdoc />
    public void RecordChanges(IEnumerable<ChangeRecord> changes) { }

    /// <inheritdoc />
    public Task RecordChangesAsync(IEnumerable<ChangeRecord> changes, CancellationToken ct = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<ChangeRecord?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<ChangeRecord?>(null);

    /// <inheritdoc />
    public Task<IReadOnlyList<ChangeRecord>> GetForEntityAsync(EntityRef forEntity, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ChangeRecord>>([]);

    /// <inheritdoc />
    public Task<IReadOnlyList<ChangeRecord>> GetForEntityTypeAsync(string forEntityType, string? forEntityId = null, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ChangeRecord>>([]);

    /// <inheritdoc />
    public Task DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default) => Task.CompletedTask;
}