using System.Linq.Expressions;
using Lyo.Common.Core;
using Lyo.Exceptions;
using Lyo.Result;
using Lyo.Result.Enums;
using Lyo.Validation.Models;

namespace Lyo.Validation;

/// <summary>Fluent builder that assembles validators for <typeparamref name="T" />.</summary>
public class ValidatorBuilder<T>
{
    private readonly List<IValidationRule<T>> _rules = [];

    /// <summary>Mints a builder.</summary>
    public static ValidatorBuilder<T> Create() => new();

    /// <summary>Appends a validation rule.</summary>
    public ValidatorBuilder<T> AddRule(IValidationRule<T> rule)
    {
        ArgumentHelpers.ThrowIfNull(rule);
        _rules.Add(rule);
        return this;
    }

    /// <summary>Appends a delegate-backed validation rule.</summary>
    public ValidatorBuilder<T> AddRule(Func<T, IReadOnlyList<Error>> validate)
    {
        ArgumentHelpers.ThrowIfNull(validate);
        _rules.Add(new ValidationRule<T>(validate));
        return this;
    }

    /// <summary>Pulls in rules from another validator.</summary>
    public ValidatorBuilder<T> Include(IValidator<T> validator)
    {
        ArgumentHelpers.ThrowIfNull(validator);
        return AddRule(instance => validator.Validate(instance).Errors ?? []);
    }

    /// <summary>Pulls in validation declared via attributes on the target type.</summary>
    public ValidatorBuilder<T> IncludeAttributes() => Include(AttributeValidator<T>.Shared);

    /// <summary>Pulls in a loaded <see cref="ValidationSchema" /> evaluated by <paramref name="evaluator" />.</summary>
    public ValidatorBuilder<T> IncludeSchema(ValidationSchema schema, IValidationClauseEvaluator evaluator)
    {
        ArgumentHelpers.ThrowIfNull(schema);
        ArgumentHelpers.ThrowIfNull(evaluator);
        ValidationSchemaCompiler.EnsureTargetType<T>(schema);
        return Include(new WhereClauseValidator<T>(schema, evaluator));
    }

    /// <summary>
    /// Loads <paramref name="key" /> from <paramref name="store" /> on the calling thread and includes it. Prefer <see cref="IValidationSchemaCompiler.GetAsync{T}" /> in async hosts.
    /// </summary>
    public ValidatorBuilder<T> IncludeStore(IValidationSchemaStore store, string key, IValidationClauseEvaluator evaluator)
    {
        ArgumentHelpers.ThrowIfNull(store);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        ArgumentHelpers.ThrowIfNull(evaluator);
        var schema = store.GetAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
        if (schema == null)
            throw new InvalidOperationException($"Validation schema '{key}' was not found.");

        return IncludeSchema(schema, evaluator);
    }

    /// <summary>Appends a model-level predicate rule.</summary>
    public ValidatorBuilder<T> Must(Func<T, bool> predicate, string errorCode, string errorMessage, IReadOnlyDictionary<string, object>? metadata = null)
    {
        ArgumentHelpers.ThrowIfNull(predicate);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(errorMessage);
        return AddRule(instance => predicate(instance) ? [] : [new(errorMessage, errorCode, metadata: metadata)]);
    }

    /// <summary>Appends a custom model-level rule that may emit more than one error.</summary>
    public ValidatorBuilder<T> Custom(Func<T, IReadOnlyList<Error>> validate)
    {
        ArgumentHelpers.ThrowIfNull(validate);
        return AddRule(validate);
    }

    /// <summary>Begins a property-level rule chain.</summary>
    public PropertyValidatorBuilder<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> selector, string? propertyName = null)
    {
        ArgumentHelpers.ThrowIfNull(selector);
        var compiled = selector.Compile();
        var resolvedPropertyName = string.IsNullOrWhiteSpace(propertyName) ? selector.Body.TryGetMemberPath() ?? selector.ToString() : propertyName!;
        if (string.IsNullOrWhiteSpace(resolvedPropertyName))
            resolvedPropertyName = selector.ToString();
        return new(this, compiled, resolvedPropertyName);
    }

    /// <summary>Builds the finished validator.</summary>
    public Validator<T> Build() => new(_rules.ToArray());

    /// <summary>Implicit conversion to the built validator.</summary>
    public static implicit operator Validator<T>(ValidatorBuilder<T> builder) => builder.Build();

    internal ValidatorBuilder<T> AddPropertyRule<TProperty>(Func<T, TProperty> selector, string propertyName, Func<T, TProperty, IReadOnlyList<Error>> validate)
    {
        ArgumentHelpers.ThrowIfNull(selector);
        ArgumentHelpers.ThrowIfNull(validate);
        return AddRule(instance => validate(instance, selector(instance)));
    }

    internal static Error CreatePropertyError(
        string propertyName,
        object? attemptedValue,
        string errorCode,
        string errorMessage,
        IReadOnlyDictionary<string, object>? metadata = null,
        ErrorSeverity severity = ErrorSeverity.Error)
    {
        var errorMetadata = new Dictionary<string, object> { [ValidationMetadataKeys.PropertyName] = propertyName, [ValidationMetadataKeys.AttemptedValue] = attemptedValue! };
        if (metadata != null) {
            foreach (var kvp in metadata)
                errorMetadata[kvp.Key] = kvp.Value;
        }

        return new(errorMessage, errorCode, metadata: errorMetadata, severity: severity);
    }
}