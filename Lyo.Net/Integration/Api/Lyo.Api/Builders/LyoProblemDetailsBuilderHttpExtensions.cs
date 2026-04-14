using System.Diagnostics;
using Lyo.Api.Models.Builders;
using Microsoft.AspNetCore.Http;

namespace Lyo.Api.Builders;

/// <summary>HTTP-aware helpers for <see cref="LyoProblemDetailsBuilder" /> (trace + RFC 7807 <c>instance</c>).</summary>
public static class LyoProblemDetailsBuilderHttpExtensions
{
    /// <summary>Populates trace/span from <see cref="Activity.Current" /> (when present) and <c>instance</c> from the request path.</summary>
    public static LyoProblemDetailsBuilder WithHttpContext(this LyoProblemDetailsBuilder builder, HttpContext? http)
    {
        if (http == null)
            return builder;

#if Net7_0_OR_GREATER
        var activity = Activity.Current;
        _ = builder.WithTrace(activity?.TraceId.ToString(), activity?.SpanId.ToString());
#endif
        var path = http.Request.Path;
        return builder.WithRoute(path.HasValue ? path.Value : null);
    }
}
