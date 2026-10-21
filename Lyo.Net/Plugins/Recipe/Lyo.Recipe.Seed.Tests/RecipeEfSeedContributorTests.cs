using Lyo.Recipe.Postgres.Database;
using Lyo.Seed;

namespace Lyo.Recipe.Seed.Tests;

public sealed class RecipeEfSeedContributorTests
{
    [Fact]
    public async Task RecipeEfSeedContributor_Count_PersistsRecipesWithLinesAndSteps()
    {
        var transport = await SeedAsync(new RecipeEfSeedContributor(), 3);
        var recipes = transport.Items.OfType<RecipeEntity>().ToList();
        var ingredients = transport.Items.OfType<IngredientEntity>().ToList();
        var lines = transport.Items.OfType<RecipeIngredientLineEntity>().ToList();
        var steps = transport.Items.OfType<RecipeStepEntity>().ToList();
        var links = transport.Items.OfType<RecipeIngredientLineStepEntity>().ToList();
        var attachments = transport.Items.OfType<RecipeAttachmentEntity>().ToList();
        var categories = transport.Items.OfType<RecipeCategoryEntity>().ToList();
        Assert.True(categories.Count > 1);
        Assert.DoesNotContain(categories, row => row.Name == "Seed");
        Assert.All(recipes, recipe => Assert.Contains(categories, row => row.Id == recipe.CategoryId));
        Assert.Equal(3, ingredients.Count);
        Assert.Equal(3, recipes.Count);
        Assert.Equal(6, lines.Count);
        Assert.Equal(6, steps.Count);
        Assert.Equal(3, links.Count);
        Assert.Equal(3, attachments.Count);
        Assert.All(
            recipes, recipe => {
                Assert.Equal(2, lines.Count(row => row.RecipeId == recipe.Id));
                Assert.Equal(2, steps.Count(row => row.RecipeId == recipe.Id));
                var line = lines.Where(row => row.RecipeId == recipe.Id).OrderBy(row => row.SortOrder).First();
                var step = steps.Where(row => row.RecipeId == recipe.Id).OrderBy(row => row.SortOrder).First();
                Assert.Contains(links, link => link.LineId == line.Id && link.StepId == step.Id);
                Assert.Contains(attachments, row => row.EntityId == recipe.Id);
            });
    }

    private static async Task<RecordingSeedTransport> SeedAsync(SeedContributor contributor, int count)
    {
        var transport = new RecordingSeedTransport();
        var result = await new SeedRunner().SeedAsync(contributor, transport, new() { Count = count, RandomSeed = 1 }, TestCt);
        Assert.True(result.Success, string.Join("; ", result.Errors));
        return transport;
    }

    private sealed class RecordingSeedTransport : ISeedTransport
    {
        public List<object> Items { get; } = [];

        public SeedTransportKind Kind => SeedTransportKind.Ef;

        public Task<bool> IsEmptyAsync<T>(CancellationToken ct = default)
            where T : class
            => Task.FromResult(!Items.OfType<T>().Any());

        public Task PersistAsync<T>(IReadOnlyList<T> items, CancellationToken ct = default)
            where T : class
        {
            foreach (var item in items)
                Items.Add(item);

            return Task.CompletedTask;
        }
    }
}