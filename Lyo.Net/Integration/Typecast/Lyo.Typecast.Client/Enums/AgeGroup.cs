using System.ComponentModel;

namespace Lyo.Typecast.Client.Enums;

/// <summary>Age group values for Typecast voices.</summary>
public enum AgeGroup
{
    /// <summary>Child</summary>
    [Description("Child")]
    Child,

    /// <summary>Teenager</summary>
    [Description("Teenager")]
    Teenager,

    /// <summary>Young adult</summary>
    [Description("Young Adult")]
    YoungAdult,

    /// <summary>Adult</summary>
    [Description("Adult")]
    Adult,

    /// <summary>Middle age</summary>
    [Description("Middle Age")]
    MiddleAge,

    /// <summary>Senior</summary>
    [Description("Senior")]
    Senior,

    /// <summary>Elder</summary>
    [Description("Elder")]
    Elder
}