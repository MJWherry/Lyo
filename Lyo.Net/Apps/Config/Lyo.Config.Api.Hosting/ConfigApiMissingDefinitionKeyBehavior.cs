namespace Lyo.Config.Api.Hosting;

/// <summary>Handling when resolved config lacks the definition key backing an options type.</summary>
public enum ConfigApiMissingDefinitionKeyBehavior
{
    /// <summary>Treat absent / null typed value as an error (<see cref="InvalidOperationException" />).</summary>
    Throw,

    /// <summary>Return <c>new TOptions()</c> when absent or deserialization yields null for reference types.</summary>
    UseDefaultInstance,
}
