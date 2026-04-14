namespace Lyo.People.Models.Enum;

/// <summary>Type of relationship between two people</summary>
public enum RelationshipType
{
    /// <summary>Spouse (married partner)</summary>
    Spouse,

    /// <summary>Domestic or romantic partner</summary>
    Partner,

    /// <summary>Parent</summary>
    Parent,

    /// <summary>Child</summary>
    Child,

    /// <summary>Sibling (brother or sister)</summary>
    Sibling,

    /// <summary>Grandparent</summary>
    GrandParent,

    /// <summary>Grandchild</summary>
    GrandChild,

    /// <summary>Employee (reports to)</summary>
    Employee,

    /// <summary>Employer (reports to this person)</summary>
    Employer,

    /// <summary>Manager or supervisor</summary>
    Manager,

    /// <summary>Colleague or coworker</summary>
    Colleague,

    /// <summary>Friend</summary>
    Friend,

    /// <summary>Emergency contact</summary>
    EmergencyContact,

    /// <summary>Other or unspecified relationship</summary>
    Other
}