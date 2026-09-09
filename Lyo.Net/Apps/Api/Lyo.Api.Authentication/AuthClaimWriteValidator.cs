using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Postgres.Database;
using Lyo.Exceptions.Models;

namespace Lyo.Api.Authentication;

/// <summary>Write-time checks for <c>[user].[claim]</c> so reserved JWT names never land in the store.</summary>
internal static class AuthClaimWriteValidator
{
    /// <summary>Throws <see cref="ValidationException" /> when type/value/user are missing or the type is reserved.</summary>
    public static void Validate(UserClaimEntity entity)
    {
        if (entity.UserId == Guid.Empty)
            throw new ValidationException("UserId", "UserId is required.");

        if (string.IsNullOrWhiteSpace(entity.Type))
            throw new ValidationException("Type", "Claim type is required.");

        if (LyoJwtClaims.IsReserved(entity.Type))
            throw new ValidationException("Type", $"Claim type '{entity.Type}' is reserved and cannot be stored.");

        if (string.IsNullOrWhiteSpace(entity.Value))
            throw new ValidationException("Value", "Claim value is required.");
    }
}
