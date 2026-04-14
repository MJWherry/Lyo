using Lyo.Api.Models;
using Lyo.Common;
using Lyo.Validation;

namespace Lyo.Api.Services.Crud.Validation;

/// <summary>Validates bulk request list length against <see cref="BulkOperationOptions.MaxAmount" /> using <see cref="Lyo.Validation" />.</summary>
public sealed record BulkListRequestValidatorInput(int Count, int MaxAllowed);

public static class BulkListRequestValidator
{
    /// <summary>Count must be within <c>[0, <see cref="BulkListRequestValidatorInput.MaxAllowed" />]</c> inclusive (upper bound comes from options per request).</summary>
    public static Result<BulkListRequestValidatorInput> Validate(BulkListRequestValidatorInput input)
    {
        var validator = ValidatorBuilder<BulkListRequestValidatorInput>.Create()
            .RuleFor(x => x.Count)
            .InclusiveBetween(
                0,
                input.MaxAllowed,
                Constants.ApiErrorCodes.ExceedMaxBulkSize,
                $"Max bulk request size is {input.MaxAllowed}, {input.Count} requests sent")
            .Build();

        return validator.Validate(input);
    }
}
