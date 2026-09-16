using Lyo.Api.Import;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;

namespace Lyo.Web.Components.Import;

/// <summary>Import form state. Duplicate targets are rejected before a file is read. Preview shows at most <see cref="PreviewCap" /> good rows.</summary>
public sealed class ImportPreviewState
{
    /// <summary>Maximum good rows drawn in the preview.</summary>
    public const int PreviewCap = 50;

    /// <summary>Shown on the preview. The component does not save.</summary>
    public const string HostPersistsCopy = "The host persists the returned rows.";

    /// <summary>Accepted source-to-target maps.</summary>
    public List<ImportFieldMap> Fields { get; } = [];

    /// <summary>Why the last map was rejected. Null when the last add succeeded.</summary>
    public string? BlockedReason { get; private set; }

    /// <summary>Labels of the first <see cref="PreviewCap" /> good rows.</summary>
    public IReadOnlyList<string> Preview { get; private set; } = [];

    /// <summary>Every issue from the import. Not capped at <see cref="PreviewCap" />.</summary>
    public IReadOnlyList<ImportIssue> Issues { get; private set; } = [];

    /// <summary>Adds a map. A second source aimed at the same target is not stored.</summary>
    public bool TryAddField(ImportFieldMap field)
    {
        foreach (var existing in Fields) {
            if (string.Equals(existing.Target, field.Target, StringComparison.Ordinal)) {
                BlockedReason = $"Two sources map to '{field.Target}'.";
                return false;
            }
        }

        Fields.Add(field);
        BlockedReason = null;
        return true;
    }

    /// <summary>Fills the preview and the issue list from an import that already ran.</summary>
    public void Show<T>(ImportResult<T> result, Func<T, string> label)
    {
        Preview = result.Rows.Take(PreviewCap).Select(label).ToArray();
        Issues = result.Issues;
    }

    /// <summary>Text the issue list renders, one line per issue.</summary>
    public string IssueListText()
    {
        if (Issues.Count == 0)
            return string.Empty;

        return string.Join('\n', Issues.Select(issue => $"{issue.Index}: {issue.Message}"));
    }
}
