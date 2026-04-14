using System.ComponentModel;

namespace Lyo.Common.Enums;

/// <summary>OMB race categories + Unknown, Two or more</summary>
public enum Race
{
    /// <summary>Unknown / Not Reported</summary>
    [Description("Unknown / Not Reported")]
    U = 0,

    /// <summary>White</summary>
    [Description("White")]
    W = 1,

    /// <summary>Black or African American</summary>
    [Description("Black or African American")]
    B = 2,

    /// <summary>Asian</summary>
    [Description("Asian")]
    A = 3,

    /// <summary>American Indian or Alaska Native</summary>
    [Description("American Indian or Alaska Native")]
    N = 4,

    /// <summary>Native Hawaiian or Other Pacific Islander</summary>
    [Description("Native Hawaiian or Other Pacific Islander")]
    H = 5,

    /// <summary>Two or More Races</summary>
    [Description("Two or More Races")]
    D = 6
}