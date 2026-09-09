using Lyo.Privacy.Policy;

namespace Lyo.Privacy.Enums;

public enum PhoneMaskMode
{
    /// <summary>Swap the match for <see cref="RedactionPolicy.Placeholder" />.</summary>
    Full,

    /// <summary>
    /// Keep the last <c>N</c> digits (<c>N</c> passed to <see cref="PhoneRedactionRule.PhoneRedactionRule(PhoneMaskMode,int,int)" />); remaining
    /// digit positions become <c>*</c>.
    /// </summary>
    LastDigits,

    /// <summary>Among the last <c>N</c> digits, keep only the first digit of that group; output is digits only (separators omitted).</summary>
    FirstDigitOfLastGroup
}