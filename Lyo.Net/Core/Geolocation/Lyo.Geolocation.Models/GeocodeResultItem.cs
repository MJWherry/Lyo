using System.Diagnostics;

namespace Lyo.Geolocation.Models;

[DebuggerDisplay("{ToString(),nq}")]
public class GeocodeResultItem
{
    public int Index { get; set; }

    public string OriginalQuery { get; set; } = string.Empty;

    public bool IsSuccess { get; set; }

    public GeocodeResult? Result { get; set; }

    public string? ErrorMessage { get; set; }

    public override string ToString()
        => IsSuccess
            ? $"GeocodeResultItem #{Index}: success, query='{OriginalQuery}', {Result}"
            : $"GeocodeResultItem #{Index}: failed, query='{OriginalQuery}', error='{ErrorMessage}'";
}
