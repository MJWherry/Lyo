using Microsoft.Extensions.Options;

namespace Lyo.Sms.Twilio;

/// <summary>Validator for TwilioOptions to ensure required properties are set.</summary>
public sealed class TwilioOptionsValidator : IValidateOptions<TwilioOptions>
{
    /// <summary>Validates the TwilioOptions instance.</summary>
    /// <param name="name">The options name (optional, used for named options).</param>
    /// <param name="options">The options instance to validate.</param>
    /// <returns>A ValidateOptionsResult indicating success or failure with error messages.</returns>
    public ValidateOptionsResult Validate(string? name, TwilioOptions options)
    {
        if (options is null)
            return ValidateOptionsResult.Fail("TwilioOptions cannot be null.");

        if (string.IsNullOrWhiteSpace(options.AccountSid))
            return ValidateOptionsResult.Fail("TwilioOptions.AccountSid is required.");

        if (string.IsNullOrWhiteSpace(options.AuthToken))
            return ValidateOptionsResult.Fail("TwilioOptions.AuthToken is required.");

        return ValidateOptionsResult.Success;
    }
}