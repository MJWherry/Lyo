namespace Lyo.People.Models;

/// <summary>Kind of employment arrangement.</summary>
public enum EmploymentType
{
    /// <summary>Full-time role.</summary>
    FullTime,

    /// <summary>Part-time role.</summary>
    PartTime,

    /// <summary>Contract or temporary role.</summary>
    Contract,

    /// <summary>Internship role.</summary>
    Internship,

    /// <summary>Freelance or self-employed work.</summary>
    Freelance,

    /// <summary>Unpaid volunteer work.</summary>
    Volunteer,

    /// <summary>Unspecified or other arrangement.</summary>
    Other
}
