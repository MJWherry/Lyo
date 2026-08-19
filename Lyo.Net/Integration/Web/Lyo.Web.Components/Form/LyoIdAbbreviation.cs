namespace Lyo.Web.Components.Form;

/// <summary>Which end of an identifier <see cref="LyoIdField" /> keeps when abbreviating.</summary>
public enum LyoIdAbbreviation
{
    /// <summary>Show the full value. No expand toggle.</summary>
    None = 0,

    /// <summary>Keep the first <see cref="LyoIdField.AbbreviationLength" /> characters.</summary>
    Prefix = 1,

    /// <summary>Keep the last <see cref="LyoIdField.AbbreviationLength" /> characters.</summary>
    Suffix = 2
}
