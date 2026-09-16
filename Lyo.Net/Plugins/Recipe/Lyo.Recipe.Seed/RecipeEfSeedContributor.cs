using Bogus;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Recipe.Models;
using Lyo.Recipe.Postgres;
using Lyo.Recipe.Postgres.Database;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Recipe.Seed;

/// <summary>EF seed of several categories, then ingredients and recipes with two lines, two steps, a line-to-step link, and one attachment each. Schema <c>recipe</c>.</summary>
public sealed class RecipeEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "Recipe";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresRecipeOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<RecipeDbContext>()
            .UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .Options;
        return new RecipeDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTime.UtcNow;
        var categoryCount = Math.Min(4, Math.Max(1, options.Count));
        var categories = Enumerable.Range(0, categoryCount).Select(i => {
            var name = faker.Commerce.Department();
            return new RecipeCategoryEntity {
                Id = LyoGuid.CreateV7(),
                Name = name,
                Slug = $"{Slug(name)}-{i}",
                CreatedTimestamp = now
            };
        }).ToArray();
        var ingredientIds = Enumerable.Range(0, options.Count).Select(_ => LyoGuid.CreateV7()).ToArray();
        graph.Entity(categories);
        graph.Entity(options.Count, i => new IngredientEntity {
            Id = ingredientIds[i],
            Name = faker.Commerce.ProductMaterial(),
            Slug = $"ingredient-{i}",
            DefaultUnit = "cup",
            CreatedTimestamp = now
        });
        var recipes = graph.Entity(options.Count, i => new RecipeEntity {
            Id = LyoGuid.CreateV7(),
            Title = faker.Commerce.ProductName(),
            Slug = $"recipe-{i}",
            CategoryId = categories[i % categoryCount].Id,
            Servings = 4,
            Status = 1,
            CreatedTimestamp = now
        });
        recipes.ForEach(_ => 2, (recipe, n) => new RecipeIngredientLineEntity {
            Id = LyoGuid.CreateV7(),
            RecipeId = recipe.Id,
            IngredientId = ingredientIds[n % ingredientIds.Length],
            SortOrder = n,
            Quantity = n + 1,
            Unit = "cup"
        });
        var units = new (string Name, string Abbreviation)[] {
            ("Teaspoon", "tsp"), ("Tablespoon", "tbsp"), ("Cup", "cup"), ("Milliliter", "ml"),
            ("Gram", "g"), ("Ounce", "oz"), ("Pound", "lb"), ("Pinch", "pinch")
        };
        graph.Entity(units.Length, i => new UnitEntity {
            Id = LyoGuid.CreateV7(),
            Name = units[i].Name,
            Abbreviation = units[i].Abbreviation,
            SortOrder = i
        });
        recipes.ForEach(_ => 2, (recipe, n) => new RecipeStepEntity {
            Id = LyoGuid.CreateV7(),
            RecipeId = recipe.Id,
            SortOrder = n,
            Instruction = n == 0 ? $"Stir in the {faker.Commerce.ProductMaterial()}." : $"Cook until the {faker.Commerce.ProductMaterial()} is done."
        });
        graph.After(async (session, ct) => {
            var links = new List<RecipeIngredientLineStepEntity>();
            var attachments = new List<RecipeAttachmentEntity>();
            foreach (var recipe in session.Generated<RecipeEntity>()) {
                var line = session.Generated<RecipeIngredientLineEntity>().Where(row => row.RecipeId == recipe.Id).OrderBy(row => row.SortOrder).First();
                var step = session.Generated<RecipeStepEntity>().Where(row => row.RecipeId == recipe.Id).OrderBy(row => row.SortOrder).First();
                links.Add(new RecipeIngredientLineStepEntity { LineId = line.Id, StepId = step.Id });
                attachments.Add(new RecipeAttachmentEntity {
                    Id = LyoGuid.CreateV7(),
                    EntityType = RecipeEntityTypes.Recipe,
                    EntityId = recipe.Id,
                    FileId = LyoGuid.CreateV7(),
                    Caption = faker.Lorem.Sentence(3).TrimEnd('.'),
                    IsPrimary = true,
                    CreatedTimestamp = now
                });
            }

            await session.PersistAsync(links, ct).ConfigureAwait(false);
            await session.PersistAsync(attachments, ct).ConfigureAwait(false);
        });
    }

    private static string Slug(string value)
    {
        var chars = value.ToLowerInvariant().Select(ch => char.IsAsciiLetterOrDigit(ch) ? ch : '-').ToArray();
        var slug = new string(chars).Trim('-');
        return slug.Length == 0 ? "recipe" : slug;
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<RecipeDbContext> ef)
            throw new SeedException("Recipe OnClear requires EfSeedTransport<RecipeDbContext>.");

        var db = ef.Context;
        await db.Attachments.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.IngredientLineSteps.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.IngredientLines.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Units.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Steps.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Recipes.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Ingredients.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Categories.ExecuteUpdateAsync(s => s.SetProperty(i => i.ParentId, (Guid?)null), ct).ConfigureAwait(false);
        await db.Categories.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
