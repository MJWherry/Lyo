using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Web.Primitives;

/// <summary>
/// Read-only code surface with an optional language label, line numbers, and a copy button. Prefer it for SQL, JSON, YAML, and C# instead of a bare
/// <c>&lt;pre&gt;</c>, which is what <c>WhereClauseViewDialog</c> still does today.
/// </summary>
/// <remarks>
/// Highlighting stays with the host theme: this block does not pull in a highlighter package. Line numbers are prepended as text so they copy with the code when
/// the user selects in the browser; the toolbar copy button always copies the original <see cref="Code" /> without numbers.
/// </remarks>
public partial class LyoCodeBlock
{
    /// <summary>Source text to display.</summary>
    [Parameter]
    public string? Code { get; set; }

    /// <summary>Language label in the toolbar, for example <c>sql</c> or <c>csharp</c>.</summary>
    [Parameter]
    public string? Language { get; set; }

    /// <summary>Prepends 1-based line numbers. Enabled by default.</summary>
    [Parameter]
    public bool LineNumbers { get; set; } = true;

    /// <summary>CSS class on the wrapper.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Inline style on the wrapper.</summary>
    [Parameter]
    public string? Style { get; set; }

    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    private string Formatted => LineNumbers ? WithLineNumbers(Code) : Code ?? string.Empty;

    private async Task CopyAsync()
    {
        if (Services.GetService<IJsInterop>() is not { } js || string.IsNullOrEmpty(Code))
            return;

        await js.SendToClipboard(Code);
    }

    private static string WithLineNumbers(string? code)
    {
        if (string.IsNullOrEmpty(code))
            return string.Empty;

        var lines = code.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var width = lines.Length.ToString().Length;
        var builder = new StringBuilder(code.Length + lines.Length * (width + 2));
        for (var i = 0; i < lines.Length; i++) {
            if (i > 0)
                builder.Append('\n');

            builder.Append((i + 1).ToString().PadLeft(width)).Append("  ").Append(lines[i]);
        }

        return builder.ToString();
    }
}
