using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

/// <summary>A person alternate date-of-birth value and derived age.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DateOfBirth(string? Dob, int Age)
{
    public override string ToString() => $"DateOfBirth: Dob='{Dob}', Age={Age}";
}