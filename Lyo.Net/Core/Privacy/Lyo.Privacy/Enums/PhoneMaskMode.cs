using Lyo.Privacy.Policy;

namespace Lyo.Privacy.Enums;

public enum PhoneMaskMode
{
    /// <summary>Replace the match with <see cref="RedactionPolicy.Placeholder" />.</summary>
    Full,

    /// <summary>Show the last <c>N</c> digits (<c>N</c> passed to <see cref="PhoneRedactionRule.PhoneRedactionRule(PhoneMaskMode,int,int)" />); other digit positions become <c>*</c>.</summary>
    LastDigits,

    /// <summary>Among the last <c>N</c> digits, show only the first digit of that group; output uses digits only (separators omitted).</summary>
    FirstDigitOfLastGroup
}