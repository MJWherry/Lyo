namespace Lyo.ClamAv.Client;

/// <summary>Verdict returned by <see cref="IClamAvClient.ScanAsync" />.</summary>
/// <param name="Status">Clean, infected, or an engine status that is neither.</param>
/// <param name="Signature">Signature name when <paramref name="Status" /> is <see cref="ClamScanStatus.Infected" />.</param>
public sealed record ClamScanResult(ClamScanStatus Status, string? Signature);
