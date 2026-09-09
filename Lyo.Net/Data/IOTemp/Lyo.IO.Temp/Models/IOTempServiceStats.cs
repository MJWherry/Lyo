using System.Diagnostics;
using Lyo.Common.Metadata.Records;

namespace Lyo.IO.Temp.Models;

/// <summary>Totals for a live <see cref="Lyo.IO.Temp.IIOTempService" /> instance.</summary>
/// <param name="ActiveSessionCount">Sessions created and not yet disposed.</param>
/// <param name="KeyedSessionCount">Entries currently in the keyed session pool.</param>
/// <param name="TotalBytesUsed">Sum of <see cref="IIOTempSession.GetTotalBytesUsed" /> over active sessions at this instant.</param>
/// <param name="ServiceDirectory">Path of the service instance folder.</param>
[DebuggerDisplay("{ToString(),nq}")]
public record IOTempServiceStats(int ActiveSessionCount, int KeyedSessionCount, long TotalBytesUsed, string ServiceDirectory)
{
    public override string ToString()
        => $"Active={ActiveSessionCount} Keyed={KeyedSessionCount} TotalUsed={FileSizeUnitInfo.FormatBestFitAbbreviation(TotalBytesUsed)} ServiceDir={ServiceDirectory}";
}