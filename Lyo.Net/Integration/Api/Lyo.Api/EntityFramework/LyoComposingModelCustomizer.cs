using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Lyo.Api.EntityFramework;

/// <summary>
/// Runs the default model build (<c>OnModelCreating</c>), then applies <see cref="CrossSchemaNavigationOptionsExtension" /> registrations so hosts can add same-DB /
/// cross-schema navigations without editing the context.
/// </summary>
public sealed class LyoComposingModelCustomizer : RelationalModelCustomizer
{
    /// <summary>Creates a customizer with EF Core dependencies.</summary>
    public LyoComposingModelCustomizer(ModelCustomizerDependencies dependencies)
        : base(dependencies) { }

    /// <inheritdoc />
    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);
        var options = context.GetService<IDbContextOptions>();
        var extension = options?.FindExtension<CrossSchemaNavigationOptionsExtension>();
        if (extension is null || extension.Registrations.Count == 0)
            return;

        foreach (var registration in extension.Registrations)
            registration.Apply(modelBuilder);
    }
}