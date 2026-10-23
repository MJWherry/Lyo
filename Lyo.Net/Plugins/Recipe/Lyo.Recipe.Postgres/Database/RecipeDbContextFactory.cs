using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Recipe.Postgres.Database;

/// <summary>EF migrations factory for EF Core migrations.</summary>
public class RecipeDbContextFactory : IDesignTimeDbContextFactory<RecipeDbContext>
{
    /// <inheritdoc />
    public RecipeDbContext CreateDbContext(string[] args) => new(PostgresDesignTime.CreateOptions<RecipeDbContext>("RECIPE_CONNECTION_STRING", PostgresRecipeOptions.Schema));
}