using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Recipe.Postgres.Database;

public sealed class RecipeIngredientLineStepEntityConfiguration : IEntityTypeConfiguration<RecipeIngredientLineStepEntity>
{
    public void Configure(EntityTypeBuilder<RecipeIngredientLineStepEntity> builder)
    {
        builder.ToTable("ingredient_line_step");
        builder.HasKey(e => new { e.LineId, e.StepId });
        builder.Property(e => e.LineId).HasColumnName("line_id").HasColumnType("uuid");
        builder.Property(e => e.StepId).HasColumnName("step_id").HasColumnType("uuid");
        builder.HasOne<RecipeIngredientLineEntity>().WithMany().HasForeignKey(e => e.LineId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RecipeStepEntity>().WithMany().HasForeignKey(e => e.StepId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.StepId).HasDatabaseName("ix_recipe_line_step_step");
    }
}