namespace Lyo.Authentication.Exceptions;

/// <summary>Thrown when callers try to issue a token with a scope name not declared via <see cref="Scopes.IScopeRegistry" />.</summary>
public sealed class ScopeNotRegisteredException : Exception
{
    /// <summary>Offending scope name.</summary>
    public string Scope { get; }

    /// <summary>Builds a new exception for the given scope.</summary>
    public ScopeNotRegisteredException(string scope)
        : base($"Scope '{scope}' is not registered. Call AddScope(\"{scope}\", ...) at startup.")
        => Scope = scope;
}