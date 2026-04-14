using System.ComponentModel;

// ReSharper disable InconsistentNaming

namespace Lyo.Common.Enums;

/// <summary> Represents units of digital storage or data measurement. </summary>
public enum FileSizeUnit
{
    /// <summary> Byte - the basic unit of digital information. </summary>
    [Description("Byte")]
    B,

    /// <summary> Kilobyte - 1,024 bytes. </summary>
    [Description("Kilobyte")]
    KB,

    /// <summary> Megabyte - 1,024 kilobytes. </summary>
    [Description("Megabyte")]
    MB,

    /// <summary> Gigabyte - 1,024 megabytes. </summary>
    [Description("Gigabyte")]
    GB,

    /// <summary> Terabyte - 1,024 gigabytes. </summary>
    [Description("Terabyte")]
    TB,

    /// <summary> Petabyte - 1,024 terabytes. </summary>
    [Description("Petabyte")]
    PB,

    /// <summary> Exabyte - 1,024 petabytes. </summary>
    [Description("Exabyte")]
    EB
}