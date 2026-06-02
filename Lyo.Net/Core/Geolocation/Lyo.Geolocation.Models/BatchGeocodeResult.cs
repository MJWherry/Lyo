using System.Diagnostics;

namespace Lyo.Geolocation.Models;

[DebuggerDisplay("{ToString(),nq}")]
public class BatchGeocodeResult
{
    public int TotalRequests { get; set; }

    public int SuccessfulResults { get; set; }

    public int FailedResults { get; set; }

    public IEnumerable<GeocodeResultItem>? Results { get; set; }

    public TimeSpan ProcessingTime { get; set; }

    public override string ToString()
        => $"BatchGeocodeResult: {SuccessfulResults}/{TotalRequests} ok, {FailedResults} failed, {ProcessingTime.TotalMilliseconds:0.#}ms";
}
