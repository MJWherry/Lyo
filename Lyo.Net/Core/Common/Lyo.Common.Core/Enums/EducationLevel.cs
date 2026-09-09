using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

/// <summary>Highest school or degree completed, using Census/BLS attainment buckets.</summary>
public enum EducationLevel
{
    /// <summary>Not known or not reported</summary>
    [Description("Unknown / Not Reported")]
    U = 0,

    /// <summary>Declined to answer / not disclosed</summary>
    [Description("Prefer not to say / Not disclosed")]
    ND = 1,

    /// <summary>Did not complete any schooling</summary>
    [Description("No schooling completed")]
    N = 2,

    /// <summary>Grades 1–11, or grade 12 without a diploma</summary>
    [Description("Grades 1–11 or 12th grade, no diploma")]
    E = 3,

    /// <summary>High-school diploma or GED</summary>
    [Description("High school diploma or GED")]
    H = 4,

    /// <summary>College credit without a degree</summary>
    [Description("Some college credit, no degree")]
    C = 5,

    /// <summary>Associate degree</summary>
    [Description("Associate's degree")]
    A = 6,

    /// <summary>Bachelor degree</summary>
    [Description("Bachelor's degree")]
    B = 7,

    /// <summary>Master degree</summary>
    [Description("Master's degree")]
    M = 8,

    /// <summary>Professional degree such as JD, MD, DDS, or DVM</summary>
    [Description("Professional degree (e.g., JD, MD, DDS, DVM)")]
    P = 9,

    /// <summary>Doctorate such as PhD or EdD</summary>
    [Description("Doctorate degree (e.g., PhD, EdD)")]
    D = 10
}