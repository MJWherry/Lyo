using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record AssociateSummary(
    string TahoeId,
    string Prefix,
    string FirstName,
    string MiddleName,
    string LastName,
    string Suffix,
    bool IsPrivate,
    bool IsOptedOut,
    bool IsDeceased,
    string Dob,
    int Score)
{
    public override string ToString()
        => $"AssociateSummary: TahoeId={TahoeId}, Name='{Prefix} {FirstName} {MiddleName} {LastName} {Suffix}', " +
            $"Score={Score}, Private={IsPrivate}, OptOut={IsOptedOut}, Deceased={IsDeceased}, Dob='{Dob}'";
}