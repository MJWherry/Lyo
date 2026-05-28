using System.Text.Json.Serialization;

namespace Lyo.Authentication.Web.Components.Models;

/// <summary>
/// Mirror of the JSON returned by <c>GET /tokens/kinds</c>. Tells the UI which token kinds the API understands, whether the current caller may issue each kind, and which
/// scope they would need to unlock the ones currently disallowed. The components library re-declares this shape so it can stay independent of <c>Lyo.Authentication.AspNetCore</c>.
/// </summary>
/// <param name="Kind">The kind name (<c>pat</c>, <c>svc</c>, <c>cli</c>, <c>webhook</c>).</param>
/// <param name="Description">Human-readable explanation suitable for inline help on the create form.</param>
/// <param name="Allowed">True when the caller currently holds the scope needed to mint this kind.</param>
/// <param name="RequiredScope">The scope the caller would need to mint this kind (<c>auth.tokens.write</c> for PAT, <c>auth.tokens.write.{kind}</c> for everything else).</param>
public sealed record AuthTokenKindDescriptor(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("description")]
    string Description,
    [property: JsonPropertyName("allowed")]
    bool Allowed,
    [property: JsonPropertyName("requiredScope")]
    string RequiredScope);