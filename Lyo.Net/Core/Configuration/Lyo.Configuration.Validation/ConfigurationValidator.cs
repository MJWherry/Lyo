using Lyo.Exceptions;
using Lyo.Query.Models.Exceptions;
using Lyo.Result;
using Lyo.Validation;
using Lyo.Validation.Models;
using Microsoft.Extensions.Configuration;
namespace Lyo.Configuration.Validation;

/// <summary>Default <see cref="IConfigurationValidator" />: loads schemas from the registered store and runs them against the host configuration.</summary>
public sealed class ConfigurationValidator : IConfigurationValidator
{
    private readonly IConfiguration _configuration;
    private readonly IValidationClauseEvaluator _evaluator;
    private readonly ConfigurationValidationOptions _options;
    private readonly IValidationSchemaStore _store;

    /// <summary>Builds a validator over <paramref name="configuration" /> using the schemas in <paramref name="store" />.</summary>
    /// <param name="configuration">Configuration to check.</param>
    /// <param name="store">Schema source, typically the in-memory store seeded from an API.</param>
    /// <param name="evaluator">Clause evaluator; register <see cref="ConfigurationClauseEvaluator" /> so raw-key validation resolves configuration keys.</param>
    /// <param name="options">Key-resolution and missing-schema behaviour.</param>
    public ConfigurationValidator(
        IConfiguration configuration,
        IValidationSchemaStore store,
        IValidationClauseEvaluator evaluator,
        ConfigurationValidationOptions options)
    {
        ArgumentHelpers.ThrowIfNull(configuration);
        ArgumentHelpers.ThrowIfNull(store);
        ArgumentHelpers.ThrowIfNull(evaluator);
        ArgumentHelpers.ThrowIfNull(options);
        _configuration = configuration;
        _store = store;
        _evaluator = evaluator;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<Result<TOptions>> ValidateSectionAsync<TOptions>(string schemaKey, string? sectionName = null, CancellationToken ct = default)
        where TOptions : class, new()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(schemaKey);
        if (ResolveSectionName(sectionName) is not { Length: > 0 } section)
            throw new ConfigurationValidationException(
                $"Validating '{typeof(TOptions).Name}' needs a section name. Pass one, or set {nameof(ConfigurationValidationOptions)}.{nameof(ConfigurationValidationOptions.DefaultSectionName)}.");

        var bound = LyoOptions.Bind<TOptions>(_configuration, section);
        var schema = await _store.GetAsync(schemaKey, ct).ConfigureAwait(false);
        if (schema == null)
            return MissingSchema(schemaKey, bound);

        return new WhereClauseValidator<TOptions>(schema, _evaluator).Validate(bound);
    }

    /// <inheritdoc />
    public async Task<Result<IConfiguration>> ValidateKeysAsync(string schemaKey, string? sectionName = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(schemaKey);
        var section = ResolveSectionName(sectionName);
        var target = section is { Length: > 0 } name ? _configuration.GetSection(name) : _configuration;

        var schema = await _store.GetAsync(schemaKey, ct).ConfigureAwait(false);
        if (schema == null)
            return MissingSchema(schemaKey, target);

        return new WhereClauseValidator<IConfiguration>(schema, _evaluator).Validate(target);
    }

    private Result<T> MissingSchema<T>(string schemaKey, T value)
        => _options.FailWhenSchemaMissing
            ? Result<T>.Failure($"Validation schema '{schemaKey}' was not found.", ValidationErrorCodes.ValidationFailed)
            : Result<T>.Success(value);

    private string? ResolveSectionName(string? sectionName) => sectionName is { Length: > 0 } explicitName ? explicitName : _options.DefaultSectionName;
}
