using Lyo.Exceptions;
using Lyo.RequestSigning;

namespace Lyo.RequestSigning.AspNetCore;

/// <summary>Checks <c>Lyo-Signature</c> and returns 401 when the check fails. The body is rewound for the rest of the pipeline.</summary>
public sealed class RequestSignatureMiddleware(RequestDelegate next)
{
    /// <summary>Hashes the body and checks the header.</summary>
    public async Task InvokeAsync(HttpContext context, RequestSigner signer)
    {
        ArgumentHelpers.ThrowIfNull(context);
        ArgumentHelpers.ThrowIfNull(signer);
        context.Request.EnableBuffering();
        byte[] body;
        using (var buffer = new MemoryStream()) {
            await context.Request.Body.CopyToAsync(buffer, context.RequestAborted).ConfigureAwait(false);
            body = buffer.ToArray();
        }

        context.Request.Body.Position = 0;
        var request = context.Request;
        var host = request.Host.Value ?? string.Empty;
        var header = request.Headers[signer.HeaderName].ToString();
        var input = new RequestSignatureInput(request.Method, request.Scheme, host, request.Path + request.QueryString, body);
        var status = signer.Verify(input, string.IsNullOrEmpty(header) ? null : header);
        if (status != RequestSignatureStatus.Valid) {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context).ConfigureAwait(false);
    }
}
