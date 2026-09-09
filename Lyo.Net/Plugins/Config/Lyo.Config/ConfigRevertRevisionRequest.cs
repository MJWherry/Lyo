namespace Lyo.Config;

/// <summary>JSON payload for Config.Api revert routes (<c>POST …/revert</c>).</summary>
/// <param name="Revision">1-based revision to restore; the store appends a new row with that snapshot.</param>
public sealed record ConfigRevertRevisionRequest(int Revision);
