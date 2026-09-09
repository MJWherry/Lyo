using Lyo.Authentication.Postgres.Database;
using Lyo.Exceptions.Models;

namespace Lyo.Api.Authentication;

/// <summary>Write-time checks for <c>[user].[scope]</c>.</summary>
internal static class AuthScopeWriteValidator
{
    /// <summary>Throws <see cref="ValidationException" /> when user or name are missing.</summary>
    public static void Validate(UserScopeEntity entity)
    {
        if (entity.UserId == Guid.Empty)
            throw new ValidationException("UserId", "UserId is required.");

        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ValidationException("Name", "Scope name is required.");

        entity.Name = entity.Name.Trim();
    }
}
