namespace Lyo.StateMachine.Models;

/// <summary>Saves and loads <see cref="StateMachineDocument" />s as append-only revisions.</summary>
/// <remarks>
/// Revisions are never overwritten, so a bad publish can be rolled back by saving the previous document again. Check
/// <c>StateMachineDocumentCompatibility.FindRemovedStates</c> before publishing a revision that drops states saved instances may still be in.
/// </remarks>
public interface IStateMachineDefinitionStore
{
    /// <summary>Saves <paramref name="document" /> as revision <paramref name="expectedRevision" /> + 1.</summary>
    /// <param name="document">Document to save. Its <see cref="StateMachineDocument.Revision" /> is ignored.</param>
    /// <param name="expectedRevision">Latest revision the caller saw, or 0 for a new machine.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>False when the latest stored revision is not <paramref name="expectedRevision" />; nothing is written.</returns>
    Task<bool> SaveAsync(StateMachineDocument document, int expectedRevision, CancellationToken ct = default);

    /// <summary>Returns a revision, or the latest when <paramref name="revision" /> is null. Null when not found.</summary>
    Task<StateMachineDocument?> GetAsync(string name, int? revision = null, CancellationToken ct = default);

    /// <summary>Names of all stored machines, ordered.</summary>
    Task<IReadOnlyList<string>> ListAsync(CancellationToken ct = default);

    /// <summary>Stored revisions of a machine, ascending.</summary>
    Task<IReadOnlyList<int>> ListRevisionsAsync(string name, CancellationToken ct = default);

    /// <summary>Deletes every revision of a machine. Returns false when none existed.</summary>
    Task<bool> DeleteAsync(string name, CancellationToken ct = default);
}