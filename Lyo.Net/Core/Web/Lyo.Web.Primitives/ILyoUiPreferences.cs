namespace Lyo.Web.Primitives;

/// <summary>
/// Small per-user string store for UI state that should survive a reload but does not belong in the URL, such as which tab was last open. Implemented by
/// <c>ClientStore</c> in <c>Lyo.Web.Primitives</c> over browser local storage.
/// </summary>
/// <remarks>
/// Components resolve this with <c>GetService</c> rather than injecting it, so a host that never registered an implementation still works and simply forgets the
/// preference between visits. Register it alongside the store with <c>AddLyoClientStore</c>.
/// </remarks>
public interface ILyoUiPreferences
{
    /// <summary>Reads a stored value, or null when the key was never written.</summary>
    /// <param name="key">Storage key. Prefix it with the component to avoid collisions, for example <c>pref_tab_job-detail</c>.</param>
    Task<string?> GetPreferenceAsync(string key);

    /// <summary>Writes a value for the current user.</summary>
    /// <param name="key">Storage key.</param>
    /// <param name="value">Value to persist.</param>
    Task SetPreferenceAsync(string key, string value);

    /// <summary>Removes a stored value so the component falls back to its default.</summary>
    /// <param name="key">Storage key.</param>
    Task RemovePreferenceAsync(string key);
}
