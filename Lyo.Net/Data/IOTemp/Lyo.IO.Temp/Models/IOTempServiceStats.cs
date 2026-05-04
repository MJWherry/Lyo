using System.Diagnostics;
using Lyo.Common.Records;

namespace Lyo.IO.Temp.Models;

/// <summary>Aggregate statistics for a running <see cref="IOTempService" /> instance.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public record IOTempServiceStats(int ActiveSessionCount, int KeyedSessionCount, long TotalBytesUsed, string ServiceDirectory)
{
    public override string ToString() => $"Active={ActiveSessionCount} Keyed={KeyedSessionCount} TotalUsed={FileSizeUnitInfo.FormatBestFitAbbreviation(TotalBytesUsed)} ServiceDir={ServiceDirectory}";
};