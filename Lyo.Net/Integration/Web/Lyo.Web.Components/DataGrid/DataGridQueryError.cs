using Lyo.Api.Client;
using Lyo.Api.Models;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Error;

namespace Lyo.Web.Components.DataGrid;

/// <summary>Maps a failed grid query onto <see cref="LyoProblemDetails" /> so the empty-state can swap its message.</summary>
internal static class DataGridQueryError
{
    /// <summary>Problem details from a thrown client/transport exception (API down, HTTP error, timeout).</summary>
    public static LyoProblemDetails FromException(Exception ex)
        => ex is ApiException { ProblemDetails: { } problem } ? problem : LyoProblemDetailsBuilder.FromException(ex).Build();

    /// <summary>Uses the query payload error, or a fallback when <c>isSuccess</c> is false with no problem details.</summary>
    public static LyoProblemDetails? FromQueryResult(bool isSuccess, LyoProblemDetails? error)
    {
        if (error is not null)
            return error;
        if (!isSuccess)
            return LyoProblemDetails.FromCode(Constants.ApiErrorCodes.Unknown, "The query failed.");
        return null;
    }
}
