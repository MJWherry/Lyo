namespace Lyo.Authentication.Models.Request;

/// <summary>Create or update payload for a user-scope row.</summary>
public sealed class AuthScopeReq
{
    /// <summary>Owning Lyo user. Required on create.</summary>
    public Guid UserId { get; set; }

    /// <summary>Scope name (e.g. <c>people.read</c>). Unique per user.</summary>
    public string Name { get; set; } = string.Empty;
}
