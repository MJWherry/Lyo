using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

/// <summary>USDA/NFC Veterans Status Table and veterans-preference codes.</summary>
public enum VeteranStatus
{
    /// <summary>Has no veteran status.</summary>
    [Description("Not a Veteran")]
    X = 0,

    /// <summary>Veteran whose service era is not known.</summary>
    [Description("Veteran, Era Unknown")]
    A = 1,

    /// <summary>Veteran from before the Vietnam era.</summary>
    [Description("Pre‑Vietnam‑era Veteran")]
    B = 2,

    /// <summary>Veteran from the Vietnam era.</summary>
    [Description("Vietnam‑era Veteran")]
    V = 3,

    /// <summary>Veteran from after the Vietnam era.</summary>
    [Description("Post‑Vietnam‑era Veteran")]
    P = 4,

    /// <summary>Not required to report veteran status.</summary>
    [Description("Exempt from Reporting")]
    E = 5,

    /// <summary>Other protected veteran (when another standard uses this code).</summary>
    [Description("Other Protected Veteran")]
    OV = 6,

    /// <summary>Veteran with a disability.</summary>
    [Description("Disabled Veteran")]
    DV = 7,

    /// <summary>Veteran with a special-disabled classification.</summary>
    [Description("Special Disabled Veteran")]
    SV = 8,

    /// <summary>Active-duty wartime or campaign-badge veteran.</summary>
    [Description("Active Duty Wartime or Campaign Badge Veteran")]
    EV = 9,

    /// <summary>Declined to answer / not disclosed.</summary>
    [Description("Not Disclosed / Prefer not to say")]
    ND = 10
}