using System.Collections.Generic;
using Lyo.Authentication.Models.Scopes;

namespace Lyo.Authentication.Scopes;

/// <summary>Registry of all declared authorization scopes for a host. Populated at startup via <see cref="ScopeRegistrationExtensions"/>.</summary>
public interface IScopeRegistry
{
    /// <summary>All currently-registered scopes, in registration order.</summary>
    IReadOnlyList<Scope> All { get; }

    /// <summary>Returns the scope with the given name, or <c>null</c> if unregistered.</summary>
    Scope? TryGet(string name);

    /// <summary>True if <paramref name="name"/> has been declared.</summary>
    bool IsRegistered(string name);

    /// <summary>
    /// Expands a caller-provided list of scope names into their transitive closure under <see cref="Scope.Implies"/>. Unknown scope names cause <see cref="Exceptions.ScopeNotRegisteredException"/>.
    /// </summary>
    IReadOnlyCollection<string> Expand(IEnumerable<string> names);
}
