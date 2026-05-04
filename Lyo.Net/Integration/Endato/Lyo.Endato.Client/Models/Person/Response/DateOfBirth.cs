using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record DateOfBirth(string? Dob, int Age)
{
    public override string ToString()
        => $"DateOfBirth: Dob='{Dob}', Age={Age}";
}