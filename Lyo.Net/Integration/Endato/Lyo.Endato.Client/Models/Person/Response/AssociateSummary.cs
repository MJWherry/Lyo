namespace Lyo.Endato.Client.Models.Person.Response;

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
    int Score);