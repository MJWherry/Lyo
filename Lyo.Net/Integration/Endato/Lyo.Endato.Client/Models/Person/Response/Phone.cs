namespace Lyo.Endato.Client.Models.Person.Response;

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
);