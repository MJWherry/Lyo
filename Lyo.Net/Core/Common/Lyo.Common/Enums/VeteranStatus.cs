using System.ComponentModel;

namespace Lyo.Common.Enums;

/// <summary>Veterans Status Table / Veterans Preference codes (USDA/NFC)</summary>
public enum VeteranStatus
{
    /// <summary>Not a veteran.</summary>
    [Description("Not a Veteran")]
    X = 0,

    /// <summary>Veteran, era unknown.</summary>
    [Description("Veteran, Era Unknown")]
    A = 1,

    /// <summary>Pre-Vietnam‐era veteran.</summary>
    [Description("Pre‑Vietnam‑era Veteran")]
    B = 2,

    /// <summary>Vietnam‑era veteran.</summary>
    [Description("Vietnam‑era Veteran")]
    V = 3,

    /// <summary>Post‑Vietnam‑era veteran.</summary>
    [Description("Post‑Vietnam‑era Veteran")]
    P = 4,

    /// <summary>Exempt from reporting.</summary>
    [Description("Exempt from Reporting")]
    E = 5,

    /// <summary>Other Protected Veteran status (if used in another standard).</summary>
    [Description("Other Protected Veteran")]
    OV = 6,

    /// <summary>Disabled veteran.</summary>
    [Description("Disabled Veteran")]
    DV = 7,

    /// <summary>Special disabled veteran.</summary>
    [Description("Special Disabled Veteran")]
    SV = 8,

    /// <summary>Active Duty Wartime or Campaign Badge Veteran.</summary>
    [Description("Active Duty Wartime or Campaign Badge Veteran")]
    EV = 9,

    /// <summary>Not disclosed / Prefer not to say.</summary>
    [Description("Not Disclosed / Prefer not to say")]
    ND = 10
}