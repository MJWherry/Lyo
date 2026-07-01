using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

/// <summary>Alternate date-of-birth value and derived age for a person.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DateOfBirth(string? Dob, int Age)
{
    public override string ToString() => $"DateOfBirth: Dob='{Dob}', Age={Age}";
}