namespace Lyo.Authentication.Saml;

/// <summary>Remembers assertion IDs until NotOnOrAfter so a second POST cannot replay one.</summary>
public interface ISamlAssertionReplayCache
{
    /// <summary>Returns false when <paramref name="assertionId" /> was already remembered and is still inside its window.</summary>
    bool TryRemember(string assertionId, DateTime notOnOrAfter);
}