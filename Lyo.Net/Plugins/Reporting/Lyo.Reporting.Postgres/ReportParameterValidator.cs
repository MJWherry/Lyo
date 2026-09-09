using Lyo.Common.Core.Conversion;
using Lyo.Parameters;
using Lyo.Query.Models.Parameters;
using Lyo.Reporting.Models.Request;
using Lyo.Reporting.Postgres.Database;

namespace Lyo.Reporting.Postgres;

/// <summary>Projects report definition parameters and generation values onto <see cref="LyoParameterValidator" />, which owns the actual validation rules.</summary>
internal static class ReportParameterValidator
{
    /// <inheritdoc cref="LyoParameterValidator.MaxValidationRegexLength" />
    internal const int MaxValidationRegexLength = LyoParameterValidator.MaxValidationRegexLength;

    /// <inheritdoc cref="LyoParameterValidator.RegexMatchTimeout" />
    internal static readonly TimeSpan RegexMatchTimeout = LyoParameterValidator.RegexMatchTimeout;

    public static IReadOnlyList<string> Validate(
        IReadOnlyList<ReportDefinitionParameter> definitionParameters,
        IReadOnlyList<ReportGenerationParameterReq> requestParameters,
        bool rejectUnknownKeys = false)
        => LyoParameterValidator.Validate(
            [.. definitionParameters.Select(ToSpec)], [.. requestParameters.Select(LyoParameterValueSpec.From)], rejectUnknownKeys);

    /// <summary>Projects a definition parameter entity onto the shared spec. The entity is not touched. Only the fields validation reads are copied.</summary>
    /// <param name="parameter">Definition parameter as stored in the database.</param>
    internal static LyoParameterSpec ToSpec(ReportDefinitionParameter parameter)
        => new(
            parameter.Key, parameter.Type, parameter.Required, parameter.ValidationRegex, parameter.MinLength, parameter.MaxLength, parameter.AllowedValues,
            TypeConversion.EnumOrDefault<LyoParameterDefaultKind>(parameter.DefaultKind), parameter.DefaultTemplate);
}
