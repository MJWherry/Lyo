namespace Lyo.Diagram.Models;

/// <summary>Loads and saves <see cref="DiagramDocument" /> values. Implementations include an in-memory store and PostgreSQL.</summary>
public interface IDiagramStore
{
    /// <summary>Returns the diagram with <paramref name="id" />, or <c>null</c> when it is missing.</summary>
    Task<DiagramDocument?> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns every stored diagram's id, name, and last-saved time, ordered by name. The cell payload is not included.</summary>
    Task<IReadOnlyList<DiagramSummary>> ListAsync(CancellationToken ct = default);

    /// <summary>Inserts or overwrites the diagram identified by <see cref="DiagramDocument.Id" />.</summary>
    Task SaveAsync(DiagramDocument diagram, CancellationToken ct = default);

    /// <summary>Removes the diagram with <paramref name="id" />. Returns <c>true</c> when a row was deleted.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
