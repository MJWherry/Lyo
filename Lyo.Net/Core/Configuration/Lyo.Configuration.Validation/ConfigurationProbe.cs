namespace Lyo.Configuration.Validation;

/// <summary>Single-property wrapper that hands one configuration value to the where-clause engine.</summary>
/// <remarks>
/// Configuration is a flat string store, so the query engine has no entity to reflect over. Rather than reimplement the comparison operators, the evaluator parses the
/// raw string into <typeparamref name="TValue" />, wraps it here, and rewrites the condition's field to <see cref="Value" /> — the engine then applies its own operator
/// semantics (string casing, <c>In</c> CSV handling, regex, lifted numeric comparison) unchanged. Public because the engine compiles expression trees against this type.
/// </remarks>
/// <typeparam name="TValue">Nullable CLR type the configuration string was parsed into, chosen from the rule's filter literal.</typeparam>
public sealed class ConfigurationProbe<TValue>
{
    /// <summary>Parsed configuration value, or <c>null</c> when the key is missing or does not parse as <typeparamref name="TValue" />.</summary>
    public TValue? Value { get; set; }
}
