using System.Diagnostics;

namespace Lyo.Api.Models.Common.Response;

/// <summary>Result of a cache clear / remove-keys / remove-tags call.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record CacheMutationRes(int RemovedCount)
{
    public override string ToString() => $"RemovedCount={RemovedCount}";
}
