namespace Lyo.People.Models;

/// <summary>Stable <see cref="Lyo.EntityReference.Models.EntityRef" /> type names for people provenance.</summary>
public static class PeopleSourceTypes
{
    public const string EndatoPsPerson = nameof(EndatoPsPerson);
    public const string EndatoCePerson = nameof(EndatoCePerson);
    public const string EndatoPsAddress = nameof(EndatoPsAddress);
    public const string EndatoPsPhoneNumber = nameof(EndatoPsPhoneNumber);
    public const string EndatoPsEmail = nameof(EndatoPsEmail);
    public const string GeolocationAddress = nameof(GeolocationAddress);
    public const string Manual = nameof(Manual);
    public const string Seed = nameof(Seed);
}