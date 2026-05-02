namespace Lyo.Diagnostic.Breadcrumbs;

/// <summary>Captures a bounded, ordered trail of <see cref="Breadcrumb" /> entries for the current logical scope (e.g. HTTP request).</summary>
public interface IBreadcrumbTrail
{
    /// <summary>Appends a breadcrumb after optional redaction.</summary>
    void Add(Breadcrumb breadcrumb);

    /// <summary>Appends a breadcrumb with optional string data.</summary>
    void Add(string category, string message, IReadOnlyDictionary<string, string>? data = null);

    /// <summary>Returns a copy of the trail from oldest to newest.</summary>
    IReadOnlyList<Breadcrumb> Snapshot();

    /// <summary>Removes all entries (e.g. when starting a new sub-scope).</summary>
    void Clear();
}
