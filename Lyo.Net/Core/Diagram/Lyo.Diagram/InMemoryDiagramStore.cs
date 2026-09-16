using System.Collections.Concurrent;
using Lyo.Diagram.Models;
using Lyo.Exceptions;

namespace Lyo.Diagram;

/// <summary>Thread-safe in-memory <see cref="IDiagramStore" /> for tests and hosts that do not open PostgreSQL.</summary>
public sealed class InMemoryDiagramStore : IDiagramStore
{
    private readonly ConcurrentDictionary<Guid, Entry> _diagrams = new();

    /// <inheritdoc />
    public Task<DiagramDocument?> GetAsync(Guid id, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfEmpty(id);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_diagrams.TryGetValue(id, out var entry) ? entry.Document : null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DiagramSummary>> ListAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        IReadOnlyList<DiagramSummary> listed = _diagrams.Values
            .OrderBy(entry => entry.Document.Name, StringComparer.Ordinal)
            .ThenBy(entry => entry.Document.Id)
            .Select(entry => new DiagramSummary { Id = entry.Document.Id, Name = entry.Document.Name, UpdatedTimestamp = entry.UpdatedTimestamp })
            .ToArray();
        return Task.FromResult(listed);
    }

    /// <inheritdoc />
    public Task SaveAsync(DiagramDocument diagram, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(diagram);
        ArgumentHelpers.ThrowIfEmpty(diagram.Id);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(diagram.Name);
        ArgumentHelpers.ThrowIfNull(diagram.Pages);
        ct.ThrowIfCancellationRequested();
        _diagrams[diagram.Id] = new(Copy(diagram), DateTime.UtcNow);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfEmpty(id);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_diagrams.TryRemove(id, out _));
    }

    private static DiagramDocument Copy(DiagramDocument diagram)
        => diagram with { Pages = [.. diagram.Pages.Select(Copy)] };

    private static DiagramPage Copy(DiagramPage page)
        => page with { Cells = [.. (page.Cells ?? []).Select(Copy)] };

    private static DiagramCell Copy(DiagramCell cell)
        => cell with { Geometry = cell.Geometry == null ? null : cell.Geometry with { Points = [.. cell.Geometry.Points ?? []] } };

    private sealed record Entry(DiagramDocument Document, DateTime UpdatedTimestamp);
}
