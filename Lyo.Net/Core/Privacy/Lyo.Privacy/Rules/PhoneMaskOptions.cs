using System.Diagnostics;
using System.Text;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Policy;

namespace Lyo.Privacy.Rules;

/// <summary>Configures how digits and separators are shown for <see cref="PhoneRedactionRule" /> matches.</summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class PhoneMaskOptions
{
    /// <summary>Use <see cref="RedactionPolicy.Placeholder" /> for the whole match.</summary>
    public static PhoneMaskOptions PolicyPlaceholder { get; } = new() { UsePolicyPlaceholder = true };

    /// <summary>When true, <see cref="LeadingDigitsVisible" /> and other fields are ignored.</summary>
    public bool UsePolicyPlaceholder { get; set; }

    /// <summary>Digits to show from the start of the digit run (0-based index in the digit sequence).</summary>
    public int LeadingDigitsVisible { get; set; }

    /// <summary>Digits to show from the end (e.g. 4 = “last four only”). Union with <see cref="LeadingDigitsVisible" />.</summary>
    public int TrailingDigitsVisible { get; set; }

    /// <summary>
    /// When set, ignores leading/trailing counts: among the last N digits, only the first of that window is visible (previous “first digit of last group” behaviour). Use
    /// <see cref="DigitsOnlyOutput" /> to drop separators.
    /// </summary>
    public int? OnlyFirstDigitAmongLastN { get; set; }

    /// <summary>Keep <c>+ ( ) - . space</c> when next to at least one visible digit; otherwise use <see cref="MaskChar" />.</summary>
    public bool PreserveSeparators { get; set; } = true;

    /// <summary>When true, output only digit and mask characters (no <c>+ - ( )</c>).</summary>
    public bool DigitsOnlyOutput { get; set; }

    public char MaskChar { get; set; } = '*';

    private string DebuggerDisplay
        => UsePolicyPlaceholder
            ? "PhoneMaskOptions(Placeholder)"
            : $"PhoneMaskOptions(lead={LeadingDigitsVisible}, trail={TrailingDigitsVisible}, nLast={OnlyFirstDigitAmongLastN?.ToString() ?? "∅"}, sep={PreserveSeparators}, digitsOnly={DigitsOnlyOutput}, mask='{MaskChar}')";

    /// <summary>Show only the last four digits (optional digits-only output).</summary>
    public static PhoneMaskOptions LastFourDigits(bool digitsOnly = false) => new() { TrailingDigitsVisible = 4, DigitsOnlyOutput = digitsOnly };

    /// <summary>Show the last <paramref name="trailingVisible" /> digits.</summary>
    public static PhoneMaskOptions LastNDigits(int trailingVisible, bool digitsOnly = false) => new() { TrailingDigitsVisible = trailingVisible, DigitsOnlyOutput = digitsOnly };

    /// <summary>Among the last <paramref name="n" /> digits, only the first is visible; digits-only output.</summary>
    public static PhoneMaskOptions FirstDigitOfLastNDigits(int n) => new() { OnlyFirstDigitAmongLastN = n, DigitsOnlyOutput = true, PreserveSeparators = false };

    /// <summary>Union of leading and trailing visible digit counts.</summary>
    public static PhoneMaskOptions LeadAndTrailDigits(int leadingVisible, int trailingVisible, bool digitsOnly = false)
        => new() { LeadingDigitsVisible = leadingVisible, TrailingDigitsVisible = trailingVisible, DigitsOnlyOutput = digitsOnly };

    /// <inheritdoc />
    public override string ToString()
    {
        if (UsePolicyPlaceholder)
            return "PhoneMaskOptions { UsePolicyPlaceholder = true }";

        var sb = new StringBuilder();
        sb.Append("PhoneMaskOptions { ");
        sb.Append("LeadingDigitsVisible = ")
            .Append(LeadingDigitsVisible)
            .Append(", TrailingDigitsVisible = ")
            .Append(TrailingDigitsVisible)
            .Append(", OnlyFirstDigitAmongLastN = ");

        sb.Append(OnlyFirstDigitAmongLastN?.ToString() ?? "null");
        sb.Append(", PreserveSeparators = ")
            .Append(PreserveSeparators)
            .Append(", DigitsOnlyOutput = ")
            .Append(DigitsOnlyOutput)
            .Append(", MaskChar = ")
            .Append(MaskChar)
            .Append(" }");

        return sb.ToString();
    }

    internal static PhoneMaskOptions FromLegacy(PhoneMaskMode mode, int digitsParameter)
        => mode switch {
            PhoneMaskMode.Full => PolicyPlaceholder,
            PhoneMaskMode.LastDigits => new() { TrailingDigitsVisible = digitsParameter },
            PhoneMaskMode.FirstDigitOfLastGroup => new() { OnlyFirstDigitAmongLastN = digitsParameter, DigitsOnlyOutput = true, PreserveSeparators = false },
            var _ => PolicyPlaceholder
        };
}