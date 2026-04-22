namespace Lyo.Web.Automation.Core;

/// <summary>Shared truncation for debugger-friendly <see cref="object.ToString" /> output.</summary>
internal static class AutomationDisplayText
{
    internal const int DefaultMaxLength = 120;

    internal static string Ellipsis(string? s, int maxChars = DefaultMaxLength)
    {
        if (s is null || s.Length == 0)
            return "";

        return s.Length <= maxChars ? s : s.Substring(0, maxChars) + "…";
    }

    internal static string OptionalName(string? name) => string.IsNullOrWhiteSpace(name) ? "" : $" [{name}]";
}