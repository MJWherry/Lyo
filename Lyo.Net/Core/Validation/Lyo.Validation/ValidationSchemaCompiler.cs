using Lyo.Exceptions;

namespace Lyo.Validation;

/// <summary>Default <see cref="IValidationSchemaCompiler" />: target-type check, then <see cref="WhereClauseValidator{T}" />.</summary>
public sealed class ValidationSchemaCompiler : IValidationSchemaCompiler
{
    private readonly IValidationClauseEvaluator _evaluator;
    private readonly IValidationSchemaStore _store;

    /// <summary>Creates a compiler that loads from <paramref name="store" /> and evaluates with <paramref name="evaluator" />.</summary>
    public ValidationSchemaCompiler(IValidationSchemaStore store, IValidationClauseEvaluator evaluator)
    {
        ArgumentHelpers.ThrowIfNull(store);
        ArgumentHelpers.ThrowIfNull(evaluator);
        _store = store;
        _evaluator = evaluator;
    }

    /// <inheritdoc />
    public IValidator<T> Compile<T>(ValidationSchema schema)
    {
        ArgumentHelpers.ThrowIfNull(schema);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(schema.Key);
        ArgumentHelpers.ThrowIfNull(schema.Constraints);
        EnsureTargetType<T>(schema);
        return new WhereClauseValidator<T>(schema, _evaluator);
    }

    /// <inheritdoc />
    public async Task<IValidator<T>> GetAsync<T>(string key, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        var schema = await _store.GetAsync(key, ct).ConfigureAwait(false);
        if (schema == null)
            throw new InvalidOperationException($"Validation schema '{key}' was not found.");

        return Compile<T>(schema);
    }

    internal static void EnsureTargetType<T>(ValidationSchema schema)
    {
        if (string.IsNullOrWhiteSpace(schema.TargetTypeName))
            return;

        var type = typeof(T);
        if (string.Equals(schema.TargetTypeName, type.Name, StringComparison.Ordinal) || string.Equals(schema.TargetTypeName, type.FullName, StringComparison.Ordinal))
            return;

        throw new InvalidOperationException($"Validation schema '{schema.Key}' targets '{schema.TargetTypeName}' but was compiled for '{type.FullName}'.");
    }
}
