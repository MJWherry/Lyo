using System.ComponentModel;

// ReSharper disable InconsistentNaming

namespace Lyo.Common.Core.Enums;

public enum CardinalDirection
{
    [Description("Unknown")]
    U,

    /// <summary>Northward</summary>
    [Description("North")]
    N,

    /// <summary>Northeastward</summary>
    [Description("Northeast")]
    NE,

    /// <summary>Eastward</summary>
    [Description("East")]
    E,

    /// <summary>Southeastward</summary>
    [Description("Southeast")]
    SE,

    /// <summary>Southward</summary>
    [Description("South")]
    S,

    /// <summary>Southwestward</summary>
    [Description("Southwest")]
    SW,

    /// <summary>Westward</summary>
    [Description("West")]
    W,

    /// <summary>Northwestward</summary>
    [Description("Northwest")]
    NW
}