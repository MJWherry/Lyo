using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Phone(
    string PhoneNumber,
    string Company,
    string Location,
    string PhoneType,
    bool IsConnected,
    bool IsPublic,
    string Latitude,
    string Longitude,
    int PhoneOrder,
    string FirstReportedDate,
    string LastReportedDate,
    string PublicFirstSeenDate
    //public string PublicLastSeenDate,
)
{
    public override string ToString()
        => $"Phone: '{PhoneNumber}', Type='{PhoneType}', Order={PhoneOrder}, Connected={IsConnected}, Public={IsPublic}, " +
           $"Location='{Location}'";
}