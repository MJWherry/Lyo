namespace Lyo.Endato.Client.Models.Enrichment.Response;

public sealed record Person(Name Name, string Age, IReadOnlyList<Address> Addresses, IReadOnlyList<Phone> Phones, IReadOnlyList<Email> Emails);