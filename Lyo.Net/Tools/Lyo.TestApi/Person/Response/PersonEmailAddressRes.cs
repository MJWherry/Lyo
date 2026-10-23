using System.Diagnostics;

namespace Lyo.TestApi.Person.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record PersonEmailAddressRes(Guid Id, Guid PersonId, string Address);