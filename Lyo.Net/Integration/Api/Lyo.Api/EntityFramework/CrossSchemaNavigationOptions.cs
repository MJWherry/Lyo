namespace Lyo.Api.EntityFramework;

/// <summary>DI options holding cross-schema / same-context navigations for <typeparamref name="TContext" />.</summary>
/// <typeparam name="TContext">Host <see cref="Microsoft.EntityFrameworkCore.DbContext" /> type.</typeparam>
public sealed class CrossSchemaNavigationOptions<TContext>
    where TContext : Microsoft.EntityFrameworkCore.DbContext
{
    /// <summary>Registrations applied by <see cref="LyoComposingModelCustomizer" /> after <c>OnModelCreating</c>.</summary>
    public List<CrossSchemaNavigationRegistration> Registrations { get; } = [];
}
