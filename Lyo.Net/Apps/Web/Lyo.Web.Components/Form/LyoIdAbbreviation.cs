namespace Lyo.Web.Components.Form;

/// <summary>Which end of an identifier <see cref="LyoIdField" /> retains when abbreviating.</summary>
public enum LyoIdAbbreviation
{
    /// <summary>Show the full value. No expand control.</summary>
    None = 0,

    /// <summary>Retain the first <see cref="LyoIdField.AbbreviationLength" /> characters.</summary>
    Prefix = 1,

    /// <summary>Retain the last <see cref="LyoIdField.AbbreviationLength" /> characters.</summary>
    Suffix = 2
}
