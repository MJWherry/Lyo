using Lyo.Geolocation.Models.Enums;

namespace Lyo.Geolocation.Models;

public class ValidationIssue
{
    public ValidationSeverity Severity { get; set; }

    public string Field { get; set; }

    public string Message { get; set; }

    public string Code { get; set; }
}