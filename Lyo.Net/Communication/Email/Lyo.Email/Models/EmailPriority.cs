namespace Lyo.Email.Models;

/// <summary>Outbound priority. Values match MimeKit <c>MessagePriority</c>.</summary>
public enum EmailPriority
{
    /// <summary>Normal priority.</summary>
    Normal,

    /// <summary>Non-urgent priority.</summary>
    NonUrgent,

    /// <summary>Urgent priority.</summary>
    Urgent
}
