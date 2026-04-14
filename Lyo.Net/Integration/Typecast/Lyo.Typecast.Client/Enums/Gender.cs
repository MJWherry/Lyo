using System.ComponentModel;

namespace Lyo.Typecast.Client.Enums;

/// <summary>Gender values for Typecast voices.</summary>
public enum Gender
{
    /// <summary>Male</summary>
    [Description("Male")]
    Male = 1,

    /// <summary>Female</summary>
    [Description("Female")]
    Female = 2
}