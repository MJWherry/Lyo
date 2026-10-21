namespace Lyo.Story.Models.Request;

/// <summary>Body for <c>POST /Story/graph/list</c>. Lists story headers in a tenant, optionally for one game.</summary>
public sealed class StoryListReq
{
    /// <summary>Tenant, or null for stories with no tenant.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Game key filter, or null for every game.</summary>
    public string? GameKey { get; set; }
}