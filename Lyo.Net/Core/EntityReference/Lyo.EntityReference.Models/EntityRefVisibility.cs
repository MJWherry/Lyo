namespace Lyo.EntityReference.Models;

/// <summary>Common visibility labels stored in the <c>visibility</c> column for association rows.</summary>
public static class EntityRefVisibility
{
    /// <summary>Visible only within the owning tenant/context (default for new rows).</summary>
    public const string Private = "private";

    /// <summary>Broader visibility policy (module-defined).</summary>
    public const string Public = "public";
}
