using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

/// <summary>OMB race categories, plus unknown and two-or-more.</summary>
public enum Race
{
    /// <summary>Not known or not reported</summary>
    [Description("Unknown / Not Reported")]
    U = 0,

    /// <summary>White race category</summary>
    [Description("White")]
    W = 1,

    /// <summary>Black or African American race category</summary>
    [Description("Black or African American")]
    B = 2,

    /// <summary>Asian race category</summary>
    [Description("Asian")]
    A = 3,

    /// <summary>American Indian or Alaska Native race category</summary>
    [Description("American Indian or Alaska Native")]
    N = 4,

    /// <summary>Native Hawaiian or Other Pacific Islander race category</summary>
    [Description("Native Hawaiian or Other Pacific Islander")]
    H = 5,

    /// <summary>Two or more race categories</summary>
    [Description("Two or More Races")]
    D = 6
}