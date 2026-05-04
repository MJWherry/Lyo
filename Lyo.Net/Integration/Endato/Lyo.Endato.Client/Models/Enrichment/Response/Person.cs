using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Enrichment.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Person(Name Name, string Age, IReadOnlyList<Address> Addresses, IReadOnlyList<Phone> Phones, IReadOnlyList<Email> Emails)
{
    public override string ToString()
        => $"Person: Age='{Age}', {Name}, Addresses={Addresses.Count}, Phones={Phones.Count}, Emails={Emails.Count}";
}