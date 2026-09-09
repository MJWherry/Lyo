using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

/// <summary>Relative summary attached to a Person Search result.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record RelativeSummary(
    string? TahoeId,
    string? Prefix,
    string? FirstName,
    string? MiddleName,
    string? LastName,
    string? Suffix,
    string? Dob,
    string? RelativeLevel,
    string? RelativeType,
    bool? Spouse,
    IReadOnlyList<string>? SharedHouseholdIds,
    int? Score,
    bool? OldSpouse = null)
{
    public override string ToString()
    {
        var display = string.Join(" ", new[] { Prefix, FirstName, MiddleName, LastName, Suffix }.Where(static s => !string.IsNullOrWhiteSpace(s)));
        return $"RelativeSummary: '{display}', TahoeId={TahoeId}, Level={RelativeLevel}, Type='{RelativeType}', Spouse={Spouse}, OldSpouse={OldSpouse}, Score={Score}";
    }
}