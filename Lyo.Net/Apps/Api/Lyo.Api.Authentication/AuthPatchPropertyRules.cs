using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.Models;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Error;
using ApiErrorCodes = Lyo.Api.Models.Constants.ApiErrorCodes;

namespace Lyo.Api.Authentication;

/// <summary>Patch rules that reject writes to identity and audit columns the admin UI must not change.</summary>
internal static class AuthPatchPropertyRules
{
    /// <summary>Blocks <paramref name="blocked" /> property names on Patch (403) while allowing every other key.</summary>
    public static PatchPropertyAuthorization Block(params string[] blocked)
    {
        var denied = new HashSet<string>(blocked, StringComparer.OrdinalIgnoreCase);
        return new() {
            Custom = (_, _, request, _) => {
                var forbidden = request.Properties.Keys.Where(denied.Contains).OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
                if (forbidden.Count == 0)
                    return ValueTask.FromResult(PatchPropertyAuthorizationResult.Ok(request));

                var detail = $"Not allowed to patch the following properties: {string.Join(", ", forbidden)}.";
                var error = LyoProblemDetails.FromCode(ApiErrorCodes.Forbidden, detail, DateTime.UtcNow, extensions: new() { ["disallowedProperties"] = forbidden });
                return ValueTask.FromResult(PatchPropertyAuthorizationResult.Forbidden(error));
            }
        };
    }
}
