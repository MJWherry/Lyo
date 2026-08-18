namespace Lyo.Validation;

/// <summary>Turns a <see cref="ValidationSchema" /> (or a stored key) into an <see cref="IValidator{T}" /> that uses the WhereClause engine.</summary>
public interface IValidationSchemaCompiler
{
    /// <summary>Compiles <paramref name="schema" /> for <typeparamref name="T" />. Throws when <see cref="ValidationSchema.TargetTypeName" /> is set and does not match <typeparamref name="T" />.</summary>
    IValidator<T> Compile<T>(ValidationSchema schema);

    /// <summary>Loads <paramref name="key" /> from the store and compiles it for <typeparamref name="T" />.</summary>
    Task<IValidator<T>> GetAsync<T>(string key, CancellationToken ct = default);
}
