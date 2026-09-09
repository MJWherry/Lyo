using System.ComponentModel;

namespace Lyo.Typecast.Client.Enums;

/// <summary>Typecast voices age group values.</summary>
public enum AgeGroup
{
    /// <summary>Child age group</summary>
    [Description("Child")]
    Child,

    /// <summary>Teen age group</summary>
    [Description("Teenager")]
    Teenager,

    /// <summary>Young-adult age group</summary>
    [Description("Young Adult")]
    YoungAdult,

    /// <summary>Adult age group</summary>
    [Description("Adult")]
    Adult,

    /// <summary>Middle-age group</summary>
    [Description("Middle Age")]
    MiddleAge,

    /// <summary>Senior age group</summary>
    [Description("Senior")]
    Senior,

    /// <summary>Elder age group</summary>
    [Description("Elder")]
    Elder
}