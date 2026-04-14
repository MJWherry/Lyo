namespace Lyo.Endato.Client.Models.Person.Response;

public sealed record Email(string EmailAddress, EmailEngagement? EmailEngagementData, int EmailOrdinal, bool IsPremium, int NonBusiness);