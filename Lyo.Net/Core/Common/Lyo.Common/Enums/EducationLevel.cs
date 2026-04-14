using System.ComponentModel;

namespace Lyo.Common.Enums;

/// <summary>Educational attainment — highest degree or level of school completed (Census/BLS categories).</summary>
public enum EducationLevel
{
    /// <summary>Unknown / Not Reported</summary>
    [Description("Unknown / Not Reported")]
    U = 0,

    /// <summary>Prefer not to say / Not disclosed</summary>
    [Description("Prefer not to say / Not disclosed")]
    ND = 1,

    /// <summary>No schooling completed</summary>
    [Description("No schooling completed")]
    N = 2,

    /// <summary>Grades 1 through 11 or 12th grade, no diploma</summary>
    [Description("Grades 1–11 or 12th grade, no diploma")]
    E = 3,

    /// <summary>High school diploma or GED</summary>
    [Description("High school diploma or GED")]
    H = 4,

    /// <summary>Some college credit, no degree</summary>
    [Description("Some college credit, no degree")]
    C = 5,

    /// <summary>Associate's degree</summary>
    [Description("Associate's degree")]
    A = 6,

    /// <summary>Bachelor's degree</summary>
    [Description("Bachelor's degree")]
    B = 7,

    /// <summary>Master's degree</summary>
    [Description("Master's degree")]
    M = 8,

    /// <summary>Professional degree (e.g., JD, MD, DDS, DVM)</summary>
    [Description("Professional degree (e.g., JD, MD, DDS, DVM)")]
    P = 9,

    /// <summary>Doctorate degree (e.g., PhD, EdD)</summary>
    [Description("Doctorate degree (e.g., PhD, EdD)")]
    D = 10
}