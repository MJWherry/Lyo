using System.Diagnostics;

namespace Lyo.Gateway.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class PersonPhoneNumberReq
{
    public string Number { get; set; } = string.Empty;

    public string Type { get; set; } = "Other";

    public DateOnly CreatedDate { get; set; }

    public DateOnly UpdatedDate { get; set; }

    public override string ToString() => $"PersonPhoneNumberReq: number={Number}, type={Type}";
}