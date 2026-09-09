namespace Lyo.Config.Api.Hosting;

/// <summary>What to do when resolved config has no definition key for an options type.</summary>
public enum ConfigApiMissingDefinitionKeyBehavior
{
    /// <summary>Treat a missing or null typed value as an error (<see cref="InvalidOperationException" />).</summary>
    Throw,

    /// <summary>Return <c>new TOptions()</c> when the key is missing or deserialization returns null for a reference type.</summary>
    UseDefaultInstance
}