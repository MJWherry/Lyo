using Lyo.Result;
using Microsoft.Extensions.Configuration;

namespace Lyo.Configuration.Validation;

/// <summary>Checks the host's <see cref="IConfiguration" /> against stored <c>ValidationSchema</c> rules.</summary>
/// <remarks>
/// Two shapes, both driven by the same schemas. <see cref="ValidateSectionAsync{TOptions}" /> binds a section to the options type first, so rules are written against the
/// options properties and the query engine evaluates them the same way it would for any other object. <see cref="ValidateKeysAsync" /> skips binding and evaluates the rules
/// against configuration keys directly — useful for keys no options type owns, or for validating before any options type exists.
/// </remarks>
public interface IConfigurationValidator
{
    /// <summary>Binds <paramref name="sectionName" /> onto a new <typeparamref name="TOptions" /> and checks it against the schema stored under <paramref name="schemaKey" />.</summary>
    /// <typeparam name="TOptions">Options type the section binds to.</typeparam>
    /// <param name="schemaKey">Key of the stored schema, for example <c>database.v1</c>.</param>
    /// <param name="sectionName">Section to bind. Falls back to <see cref="ConfigurationValidationOptions.DefaultSectionName" />, then the configuration root.</param>
    /// <param name="ct">Token used to cancel the check.</param>
    /// <returns>The bound options on success, or the rule failures as errors.</returns>
    Task<Result<TOptions>> ValidateSectionAsync<TOptions>(string schemaKey, string? sectionName = null, CancellationToken ct = default)
        where TOptions : class, new();

    /// <summary>Checks configuration keys directly against the schema stored under <paramref name="schemaKey" />, with no options type in between.</summary>
    /// <param name="schemaKey">Key of the stored schema.</param>
    /// <param name="sectionName">Section the rule field paths are relative to. Falls back to <see cref="ConfigurationValidationOptions.DefaultSectionName" />, then the root.</param>
    /// <param name="ct">Token used to cancel the check.</param>
    /// <returns>The checked configuration on success, or the rule failures as errors.</returns>
    Task<Result<IConfiguration>> ValidateKeysAsync(string schemaKey, string? sectionName = null, CancellationToken ct = default);
}
