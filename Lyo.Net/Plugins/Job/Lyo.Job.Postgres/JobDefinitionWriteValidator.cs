using System.ComponentModel.DataAnnotations;
using Lyo.Common.Core.Conversion;
using Lyo.Job.Postgres.Database;
using Lyo.Parameters;

namespace Lyo.Job.Postgres;

/// <summary>
/// Write-time checks for job definition parameters, so bad metadata (an uncompilable regex, an expression default with no template) fails on save rather than on every run
/// that needs it. Mirrors <c>ReportDefinitionWriteValidator</c>. Wired into the definition and definition-parameter CRUD hooks.
/// </summary>
public static class JobDefinitionWriteValidator
{
    /// <summary>Validates a single declared parameter.</summary>
    /// <param name="parameter">Parameter being created or updated.</param>
    /// <exception cref="ValidationException">The parameter's metadata is inconsistent.</exception>
    public static void ValidateParameter(JobParameter parameter)
    {
        var errors = new List<string>();
        LyoParameterValidator.ValidateSpec(ToSpec(parameter), errors);
        ThrowIfAny(errors);
    }

    /// <summary>Validates every parameter declared on a definition, plus uniqueness of their keys.</summary>
    /// <param name="parameters">Parameters declared on the definition.</param>
    /// <exception cref="ValidationException">One or more parameters are inconsistent, or a key is declared twice.</exception>
    public static void ValidateParameters(IEnumerable<JobParameter> parameters)
    {
        var errors = new List<string>();
        var specs = parameters.Select(ToSpec).ToList();
        foreach (var spec in specs)
            LyoParameterValidator.ValidateSpec(spec, errors);

        LyoParameterValidator.ValidateUniqueKeys(specs, errors);
        ThrowIfAny(errors);
    }

    /// <summary>Projects the declared shape of a stored parameter for validation and default resolution.</summary>
    /// <param name="parameter">Stored parameter to project.</param>
    public static LyoParameterSpec ToSpec(JobParameter parameter)
        => new(
            parameter.Key, parameter.Type, parameter.Required, parameter.ValidationRegex, parameter.MinLength, parameter.MaxLength, parameter.AllowedValues,
            TypeConversion.EnumOrDefault<LyoParameterDefaultKind>(parameter.DefaultKind), parameter.DefaultTemplate);

    private static void ThrowIfAny(List<string> errors)
    {
        if (errors.Count > 0)
            throw new ValidationException(string.Join(" ", errors));
    }
}
