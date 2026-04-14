namespace Lyo.Endato.Client.Models.Person.Response;

public sealed record Name(
    string Prefix,
    string FirstName,
    string MiddleName,
    string LastName,
    string Suffix,
    IReadOnlyList<string> RawNames,
    string? PublicFirstSeenDate
    //public string? PublicLastSeenDate,
);