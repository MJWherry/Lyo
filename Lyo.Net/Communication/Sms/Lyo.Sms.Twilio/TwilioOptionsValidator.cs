using Microsoft.Extensions.Options;

namespace Lyo.Sms.Twilio;

/// <summary>Checks that required TwilioOptions members are populated.</summary>
public sealed class TwilioOptionsValidator : IValidateOptions<TwilioOptions>
{
    /// <summary>Validates one TwilioOptions instance.</summary>
    /// <param name="name">Named-options id, if any.</param>
    /// <param name="options">Instance to check.</param>
    /// <returns>Success or a failure listing the problems.</returns>
    public ValidateOptionsResult Validate(string? name, TwilioOptions options)
    {
        if (options is null)
            return ValidateOptionsResult.Fail("TwilioOptions cannot be null.");

        try {
            options.Validate();
        }
        catch (ArgumentException ex) {
            return ValidateOptionsResult.Fail(ex.Message);
        }

        return ValidateOptionsResult.Success;
    }
}