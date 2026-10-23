using System.Diagnostics;

namespace Lyo.TestApi.Person.Response;

/// <summary>Small phone display DTO used when a row points at contact_phone_number_id in the Lyo people schema.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ContactPhoneNumberRes(Guid Id, string Number, string? Type);