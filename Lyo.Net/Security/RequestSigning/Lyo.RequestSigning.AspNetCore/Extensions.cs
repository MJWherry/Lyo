using Lyo.Exceptions;

namespace Lyo.RequestSigning.AspNetCore;

/// <summary>Pipeline registration for <see cref="RequestSignatureMiddleware" />.</summary>
public static class Extensions
{
    /// <param name="app">Pipeline that receives the middleware.</param>
    extension(IApplicationBuilder app)
    {
        /// <summary>Checks <c>Lyo-Signature</c> on each request. Register <see cref="RequestSigner" /> first.</summary>
        public IApplicationBuilder UseRequestSignature()
        {
            ArgumentHelpers.ThrowIfNull(app);
            return app.UseMiddleware<RequestSignatureMiddleware>();
        }
    }
}