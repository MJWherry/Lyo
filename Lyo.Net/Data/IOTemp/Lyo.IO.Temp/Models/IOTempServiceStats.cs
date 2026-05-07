using System.Diagnostics;
using Lyo.Common.Records;

namespace Lyo.IO.Temp.Models;

/// <summary>Aggregate statistics for a running <see cref="Lyo.IO.Temp.IIOTempService" /> instance.</summary>
/// <param name="ActiveSessionCount">Sessions created and not yet disposed.</param>
/// <param name="KeyedSessionCount">Entries currently held in the keyed session pool.</param>
/// <param name="TotalBytesUsed">Sum of <see cref="IIOTempSession.GetTotalBytesUsed" /> across active sessions (point-in-time).</param>
/// <param name="ServiceDirectory">The service instance directory path.</param>
[DebuggerDisplay("{ToString(),nq}")]
public record IOTempServiceStats(int ActiveSessionCount, int KeyedSessionCount, long TotalBytesUsed, string ServiceDirectory)
{
    public override string ToString()
        => $"Active={ActiveSessionCount} Keyed={KeyedSessionCount} TotalUsed={FileSizeUnitInfo.FormatBestFitAbbreviation(TotalBytesUsed)} ServiceDir={ServiceDirectory}";
}