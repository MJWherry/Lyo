using Lyo.Common.Core.Extensions;

namespace Lyo.Web.Automation.Core;

/// <summary>Common truncation for debugger-friendly <see cref="object.ToString" /> output.</summary>
internal static class AutomationDisplayText
{
    internal const int DefaultMaxLength = 120;

    internal static string Ellipsis(string? s, int maxChars = DefaultMaxLength) => s.TruncateWithEllipsis(maxChars, "…");

    internal static string OptionalName(string? name) => string.IsNullOrWhiteSpace(name) ? "" : $" [{name}]";
}