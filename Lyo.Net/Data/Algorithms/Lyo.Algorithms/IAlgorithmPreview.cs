using System.Text.Json;

namespace Lyo.Algorithms;

/// <summary>Compiles an unsaved revision and runs it against a JSON context bag. Nothing is written to the catalog.</summary>
public interface IAlgorithmPreview
{
    /// <summary>
    /// Runs <paramref name="draft" /> as <paramref name="key" />. Context names must be identifiers. Each value is converted to that name's parameter type
    /// (<see cref="double" /> when the payload does not declare it, <see cref="string" /> for a <c>_key</c> name).
    /// </summary>
    /// <param name="draft">Kind, source, and payload. The payload's <c>result</c>, <c>parameters</c>, <c>calls</c>, and <c>inputType</c> select the compile types.</param>
    /// <param name="context">JSON object of identifier to value. An empty object runs with an empty bag.</param>
    /// <param name="key">Step name. Must be a catalog key. Defaults to <c>preview</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<AlgorithmPreviewResult> PreviewAsync(AlgorithmRevisionReq draft, JsonElement context, string? key = null, CancellationToken ct = default);
}
