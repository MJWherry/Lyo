using Lyo.Job.Worker;
using Lyo.Parameters;
using Lyo.Result;
using Microsoft.Extensions.Logging;

namespace Lyo.Job.Workers.Notification;

/// <summary>
/// Shared parameter reading for the notification workers. A missing required parameter is a permanent configuration fault, so it fails the run with the key named in the
/// reason rather than throwing — retrying a definition that is missing a <c>Url</c> can never succeed, and the run log is where an operator will look.
/// </summary>
internal static class NotificationParameters
{
    /// <summary>Reads a required parameter. Returns false and fails the run (with the key in the failure reason) when it is missing or blank.</summary>
    public static bool TryRequired(IJobWorkerContext ctx, string key, out string value)
    {
        value = ctx.Run.JobRunParameters.GetString(key) ?? "";
        if (!string.IsNullOrWhiteSpace(value))
            return true;

        ctx.Results.AddError($"Required parameter '{key}' is missing or empty.");
        ctx.Logger.LogError("Notification run {RunId} is missing required parameter {ParameterKey}", ctx.Run.Id, key);
        return false;
    }

    /// <summary>Reads an optional parameter, treating blank as absent.</summary>
    public static string? Optional(IJobWorkerContext ctx, string key)
    {
        var value = ctx.Run.JobRunParameters.GetString(key);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>Splits a comma or semicolon separated recipient list.</summary>
    public static IReadOnlyList<string> Recipients(string value)
        => [.. value.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries).Select(part => part.Trim()).Where(part => part.Length > 0)];

    /// <summary>Flattens the errors on a failed send into one line for the run's failure reason.</summary>
    public static string FormatErrors(IReadOnlyList<Error>? errors) => errors is null || errors.Count == 0 ? "unknown error" : string.Join("; ", errors.Select(e => e.Message));
}