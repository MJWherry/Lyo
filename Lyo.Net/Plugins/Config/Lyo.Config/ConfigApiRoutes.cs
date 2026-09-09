using System.Globalization;

namespace Lyo.Config;

/// <summary>Relative HTTP paths for Config.Api manage and resolve routes (relative to the API host, no leading slash).</summary>
public static class ConfigApiRoutes
{
    /// <summary>Default path prefix for Config.Api (<c>/api/config</c>).</summary>
    public const string Prefix = "api/config";

    /// <summary>Write or read definitions collection.</summary>
    public const string ManageDefinitions = "api/config/manage/definitions";

    /// <summary>Write or read bindings collection.</summary>
    public const string ManageBindings = "api/config/manage/bindings";

    /// <summary>Fetch merged config for an entity.</summary>
    public const string ManageResolved = "api/config/manage/resolved";

    /// <summary>Returns definitions for <paramref name="subjectEntityType" />.</summary>
    public static string DefinitionsFor(string subjectEntityType)
        => $"{ManageDefinitions}?subjectEntityType={Uri.EscapeDataString(subjectEntityType)}";

    /// <summary>Read or delete one definition.</summary>
    public static string Definition(Guid id) => $"{ManageDefinitions}/{id:D}";

    /// <summary>Fetch a definition by entity type and key.</summary>
    public static string DefinitionByKey(string subjectEntityType, string key)
        => $"{ManageDefinitions}/by-key?subjectEntityType={Uri.EscapeDataString(subjectEntityType)}&key={Uri.EscapeDataString(key)}";

    /// <summary>Fetch definition metadata history.</summary>
    public static string DefinitionRevisions(Guid id) => $"{Definition(id)}/revisions";

    /// <summary>Fetch one definition revision.</summary>
    public static string DefinitionRevision(Guid id, int revision) => $"{DefinitionRevisions(id)}/{revision.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>Submit revert for a definition.</summary>
    public static string DefinitionRevert(Guid id) => $"{Definition(id)}/revert";

    /// <summary>Read or delete bindings for one entity.</summary>
    public static string BindingsFor(string subjectEntityType, string subjectEntityId)
        => $"{ManageBindings}?subjectEntityType={Uri.EscapeDataString(subjectEntityType)}&subjectEntityId={Uri.EscapeDataString(subjectEntityId)}";

    /// <summary>Read or delete one binding.</summary>
    public static string Binding(Guid id) => $"{ManageBindings}/{id:D}";

    /// <summary>Fetch a binding by entity and key.</summary>
    public static string BindingByKey(string subjectEntityType, string subjectEntityId, string key)
        => $"{ManageBindings}/by-key?subjectEntityType={Uri.EscapeDataString(subjectEntityType)}&subjectEntityId={Uri.EscapeDataString(subjectEntityId)}&key={Uri.EscapeDataString(key)}";

    /// <summary>Fetch value history for a binding.</summary>
    public static string BindingRevisions(Guid id) => $"{Binding(id)}/revisions";

    /// <summary>Fetch one binding revision.</summary>
    public static string BindingRevision(Guid id, int revision) => $"{BindingRevisions(id)}/{revision.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>Submit revert for a binding.</summary>
    public static string BindingRevert(Guid id) => $"{Binding(id)}/revert";

    /// <summary>Fetch resolved config for an entity.</summary>
    public static string Resolved(string subjectEntityType, string subjectEntityId)
        => $"{ManageResolved}?subjectEntityType={Uri.EscapeDataString(subjectEntityType)}&subjectEntityId={Uri.EscapeDataString(subjectEntityId)}";
}
