namespace Lyo.People.Models.Enum;

/// <summary>Kind of relationship between two people.</summary>
public enum RelationshipType
{
    /// <summary>Married partner.</summary>
    Spouse,

    /// <summary>Domestic or romantic partner.</summary>
    Partner,

    /// <summary>Parent of the person.</summary>
    Parent,

    /// <summary>Child of the person.</summary>
    Child,

    /// <summary>Brother or sister.</summary>
    Sibling,

    /// <summary>Grandparent of the person.</summary>
    GrandParent,

    /// <summary>Grandchild of the person.</summary>
    GrandChild,

    /// <summary>Employee who reports to the person.</summary>
    Employee,

    /// <summary>Employer the person reports to.</summary>
    Employer,

    /// <summary>Manager or supervisor.</summary>
    Manager,

    /// <summary>Coworker or colleague.</summary>
    Colleague,

    /// <summary>Personal friend.</summary>
    Friend,

    /// <summary>Designated emergency contact.</summary>
    EmergencyContact,

    /// <summary>Unspecified or other relationship.</summary>
    Other
}
