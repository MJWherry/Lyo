namespace Lyo.Web.Primitives;

/// <summary>CSS class and disabled-state helpers for <see cref="LyoToolbar" /> and its child controls.</summary>
internal static class LyoToolbarLayout
{
    /// <summary>Root class list. <paramref name="dense" /> tightens padding and gaps.</summary>
    public static string Root(bool dense) => dense ? "lyo-toolbar lyo-toolbar-dense" : "lyo-toolbar";

    /// <summary>Group class list. <paramref name="grow" /> takes leftover width; <paramref name="alignEnd" /> pins the group to the inline end.</summary>
    public static string Group(bool grow, bool alignEnd)
    {
        var css = "lyo-toolbar-group";
        if (grow)
            css += " lyo-toolbar-grow";
        if (alignEnd)
            css += " lyo-toolbar-end";
        return css;
    }

    /// <summary>Row class list. Collapsed rows stay in the tree so inputs are not disposed.</summary>
    public static string Row(bool collapsed) => collapsed ? "lyo-toolbar-row lyo-toolbar-row-collapsed" : "lyo-toolbar-row";

    /// <summary>Field wrapper class list.</summary>
    public static string Field(bool grow) => grow ? "lyo-toolbar-field lyo-toolbar-field-grow" : "lyo-toolbar-field";

    /// <summary>True when the control or its owning toolbar is disabled.</summary>
    public static bool IsDisabled(bool disabled, bool ownerDisabled) => disabled || ownerDisabled;
}
