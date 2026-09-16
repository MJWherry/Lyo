namespace Lyo.Recipe.Web.Components;

/// <summary>URL-friendly slugs from display names when the user does not type one.</summary>
public static class RecipeSlugs
{
    /// <summary>Lowercases <paramref name="name"/> and replaces non-alphanumerics with hyphens.</summary>
    public static string FromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var chars = name.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-');
        return string.Concat(chars).Trim('-');
    }
}
