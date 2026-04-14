using System.Linq.Expressions;
using Lyo.Common;
using Lyo.Common.Enums;
using Lyo.Exceptions;

namespace Lyo.Validation;

/// <summary>Fluent builder for creating validators for <typeparamref name="T" />.</summary>
public class ValidatorBuilder<T>
{
    private readonly List<IValidationRule<T>> _rules = new();

    /// <summary>Creates a new builder instance.</summary>
    public static ValidatorBuilder<T> Create() => new();

    /// <summary>Adds a validation rule.</summary>
    public ValidatorBuilder<T> AddRule(IValidationRule<T> rule)
    {
        ArgumentHelpers.ThrowIfNull(rule, nameof(rule));
        _rules.Add(rule);
        return this;
    }

    /// <summary>Adds a delegate-backed validation rule.</summary>
    public ValidatorBuilder<T> AddRule(Func<T, IReadOnlyList<Error>> validate)
    {
        ArgumentHelpers.ThrowIfNull(validate, nameof(validate));
        _rules.Add(new ValidationRule<T>(validate));
        return this;
    }

    /// <summary>Includes rules from another validator.</summary>
    public ValidatorBuilder<T> Include(IValidator<T> validator)
    {
        ArgumentHelpers.ThrowIfNull(validator, nameof(validator));
        return AddRule(instance => validator.Validate(instance).Errors ?? []);
    }

    /// <summary>Includes validation declared via attributes on the target type.</summary>
    public ValidatorBuilder<T> IncludeAttributes() => Include(AttributeValidator<T>.Shared);

    /// <summary>Adds a model-level predicate rule.</summary>
    public ValidatorBuilder<T> Must(Func<T, bool> predicate, string errorCode, string errorMessage, IReadOnlyDictionary<string, object>? metadata = null)
    {
        ArgumentHelpers.ThrowIfNull(predicate, nameof(predicate));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(errorCode, nameof(errorCode));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(errorMessage, nameof(errorMessage));
        return AddRule(instance => predicate(instance) ? [] : [new(errorMessage, errorCode, metadata: metadata)]);
    }

    /// <summary>Adds a custom model-level rule that can emit multiple errors.</summary>
    public ValidatorBuilder<T> Custom(Func<T, IReadOnlyList<Error>> validate)
    {
        ArgumentHelpers.ThrowIfNull(validate, nameof(validate));
        return AddRule(validate);
    }

    /// <summary>Starts a property-level rule chain.</summary>
    public PropertyValidatorBuilder<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> selector, string? propertyName = null)
    {
        ArgumentHelpers.ThrowIfNull(selector, nameof(selector));
        var compiled = selector.Compile();
        var resolvedPropertyName = !string.IsNullOrWhiteSpace(propertyName) ? propertyName : GetPropertyPath(selector.Body);
        resolvedPropertyName = string.IsNullOrWhiteSpace(resolvedPropertyName) ? selector.ToString() : resolvedPropertyName;
        return new(this, compiled, resolvedPropertyName!);
    }

    /// <summary>Builds the validator instance.</summary>
    public Validator<T> Build() => new(_rules.ToArray());

    /// <summary>Implicit conversion to a validator.</summary>
    public static implicit operator Validator<T>(ValidatorBuilder<T> builder) => builder.Build();

    internal ValidatorBuilder<T> AddPropertyRule<TProperty>(Func<T, TProperty> selector, string propertyName, Func<T, TProperty, IReadOnlyList<Error>> validate)
    {
        ArgumentHelpers.ThrowIfNull(selector, nameof(selector));
        ArgumentHelpers.ThrowIfNull(validate, nameof(validate));
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

    private static string GetPropertyPath(Expression expression)
    {
        if (expression is MemberExpression member) {
            var parent = GetPropertyPath(member.Expression!);
            return string.IsNullOrWhiteSpace(parent) ? member.Member.Name : $"{parent}.{member.Member.Name}";
        }

        if (expression is UnaryExpression unary)
            return GetPropertyPath(unary.Operand);

        if (expression is ParameterExpression)
            return string.Empty;

        return expression.ToString();
    }
}