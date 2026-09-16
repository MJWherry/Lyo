using Lyo.Exceptions;

namespace Lyo.Idempotency.AspNetCore;

/// <summary>Pipeline registration for <see cref="IdempotencyMiddleware" />.</summary>
public static class Extensions
{
    /// <param name="app">Application pipeline.</param>
    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Adds <see cref="IdempotencyMiddleware" />. Call this after <c>UseRouting</c> and after authentication.
        /// </summary>
        public IApplicationBuilder UseIdempotency()
        {
            ArgumentHelpers.ThrowIfNull(app);
            return app.UseMiddleware<IdempotencyMiddleware>();
        }
    }
}
