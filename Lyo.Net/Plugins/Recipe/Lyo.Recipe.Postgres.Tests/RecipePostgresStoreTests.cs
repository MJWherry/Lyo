using Lyo.Recipe.Models;
using Lyo.Recipe.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using Lyo.Common.Core.Identifiers;

namespace Lyo.Recipe.Postgres.Tests;

[Trait("Category", "Fast")]
public sealed class RecipePostgresStoreTests(RecipePostgresFixture fixture)
{
    [Fact]
    public async Task SaveRecipe_ThenGetById_RoundTripsFields()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IRecipeStore>();
        var category = new RecipeCategoryRecord { Name = "Desserts", Slug = Unique("desserts") };
        await store.SaveCategoryAsync(category, tenantId: null, ct);
        var savedCategory = (await store.ListCategoriesAsync(null, ct)).Single(c => c.Slug == category.Slug);
        var id = LyoGuid.CreateV7();
        await store.SaveRecipeAsync(
            new RecipeRecord {
                Id = id,
                Title = "Pie",
                Slug = Unique("pie"),
                Description = "A pie",
                Servings = 8,
                PrepMinutes = 20,
                CookMinutes = 45,
                Difficulty = RecipeDifficulty.Medium,
                Status = RecipeStatus.Active,
                SourceName = "Grandma",
                SourceUrl = "https://example.test/pie",
                YieldLabel = "one 9-inch pie",
                CategoryId = savedCategory.Id
            }, tenantId: null, ct);
        var loaded = await store.GetRecipeByIdAsync(id, tenantId: null, ct);
        Assert.NotNull(loaded);
        Assert.Equal("Pie", loaded.Title);
        Assert.Equal(8, loaded.Servings);
        Assert.Equal(20, loaded.PrepMinutes);
        Assert.Equal(45, loaded.CookMinutes);
        Assert.Equal(RecipeDifficulty.Medium, loaded.Difficulty);
        Assert.Equal(RecipeStatus.Active, loaded.Status);
        Assert.Equal("Grandma", loaded.SourceName);
        Assert.Equal("https://example.test/pie", loaded.SourceUrl);
        Assert.Equal("one 9-inch pie", loaded.YieldLabel);
        Assert.Equal(savedCategory.Id, loaded.CategoryId);
    }

    [Fact]
    public async Task ReplaceIngredientLines_RoundTripsOrderAndOptionalQuantity()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IRecipeStore>();
        var flour = new IngredientRecord { Name = "Flour", Slug = Unique("flour") };
        var salt = new IngredientRecord { Name = "Salt", Slug = Unique("salt") };
        await store.SaveIngredientAsync(flour, tenantId: null, ct);
        await store.SaveIngredientAsync(salt, tenantId: null, ct);
        var flourId = (await store.GetIngredientBySlugAsync(flour.Slug, tenantId: null, ct))!.Id;
        var saltId = (await store.GetIngredientBySlugAsync(salt.Slug, tenantId: null, ct))!.Id;
        var recipeId = LyoGuid.CreateV7();
        await store.SaveRecipeAsync(new RecipeRecord { Id = recipeId, Title = "Bread", Slug = Unique("bread") }, tenantId: null, ct);
        await store.ReplaceIngredientLinesAsync(
            recipeId,
            [
                new() { RecipeId = recipeId, IngredientId = flourId, SortOrder = 0, Quantity = 2, Unit = "cups", GroupLabel = "Dough" },
                new() { RecipeId = recipeId, IngredientId = saltId, SortOrder = 1, Quantity = null, Unit = "pinch", GroupLabel = "Dough" }
            ], tenantId: null, ct);
        var lines = await store.GetIngredientLinesAsync(recipeId, tenantId: null, ct);
        Assert.Equal(2, lines.Count);
        Assert.Equal(flourId, lines[0].IngredientId);
        Assert.Equal(2m, lines[0].Quantity);
        Assert.Equal("Dough", lines[0].GroupLabel);
        Assert.Equal(saltId, lines[1].IngredientId);
        Assert.Null(lines[1].Quantity);
        Assert.Equal("Dough", lines[1].GroupLabel);
    }

    [Fact]
    public async Task ReplaceIngredientLines_FreeTextLine_RoundTripsWithoutCatalogRow()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IRecipeStore>();
        var recipeId = LyoGuid.CreateV7();
        await store.SaveRecipeAsync(new RecipeRecord { Id = recipeId, Title = "Toast", Slug = Unique("toast") }, tenantId: null, ct);
        await store.ReplaceIngredientLinesAsync(
            recipeId,
            [new() { RecipeId = recipeId, IngredientText = "2 cups flour", SortOrder = 0 }],
            tenantId: null, ct);
        var line = Assert.Single(await store.GetIngredientLinesAsync(recipeId, tenantId: null, ct));
        Assert.Null(line.IngredientId);
        Assert.Equal("2 cups flour", line.IngredientText);
    }

    [Fact]
    public async Task ReplaceIngredientLines_NoIngredientIdOrText_Throws()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IRecipeStore>();
        var recipeId = LyoGuid.CreateV7();
        await store.SaveRecipeAsync(new RecipeRecord { Id = recipeId, Title = "Soup", Slug = Unique("soup") }, tenantId: null, ct);
        await Assert.ThrowsAsync<ArgumentException>(() => store.ReplaceIngredientLinesAsync(
            recipeId,
            [new() { RecipeId = recipeId, SortOrder = 0 }],
            tenantId: null, ct));
    }

    [Fact]
    public async Task ReplaceSteps_RoundTripsOrder()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IRecipeStore>();
        var recipeId = LyoGuid.CreateV7();
        await store.SaveRecipeAsync(new RecipeRecord { Id = recipeId, Title = "Eggs", Slug = Unique("eggs") }, tenantId: null, ct);
        await store.ReplaceStepsAsync(
            recipeId,
            [
                new() { RecipeId = recipeId, SortOrder = 1, Instruction = "Cook", DurationMinutes = 5 },
                new() { RecipeId = recipeId, SortOrder = 0, Instruction = "Crack", GroupLabel = "Prep" }
            ], tenantId: null, ct);
        var steps = await store.GetStepsAsync(recipeId, tenantId: null, ct);
        Assert.Equal(2, steps.Count);
        Assert.Equal("Crack", steps[0].Instruction);
        Assert.Equal("Prep", steps[0].GroupLabel);
        Assert.Equal("Cook", steps[1].Instruction);
        Assert.Equal(5, steps[1].DurationMinutes);
    }

    [Fact]
    public async Task DeleteRecipe_RemovesLinesStepsAndAttachmentRows()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IRecipeStore>();
        var recipeId = LyoGuid.CreateV7();
        await store.SaveRecipeAsync(new RecipeRecord { Id = recipeId, Title = "Cake", Slug = Unique("cake") }, tenantId: null, ct);
        await store.ReplaceIngredientLinesAsync(
            recipeId, [new() { RecipeId = recipeId, IngredientText = "sugar", SortOrder = 0 }], tenantId: null, ct);
        await store.ReplaceStepsAsync(
            recipeId, [new() { RecipeId = recipeId, SortOrder = 0, Instruction = "Bake" }], tenantId: null, ct);
        await store.SaveAttachmentAsync(
            new RecipeAttachmentRecord {
                EntityType = RecipeEntityTypes.Recipe,
                EntityId = recipeId,
                FileId = LyoGuid.CreateV7(),
                Role = RecipeAttachmentRole.Scan
            }, tenantId: null, ct);
        await store.DeleteRecipeAsync(recipeId, tenantId: null, ct);
        Assert.Null(await store.GetRecipeByIdAsync(recipeId, tenantId: null, ct));
        Assert.Empty(await store.GetIngredientLinesAsync(recipeId, tenantId: null, ct));
        Assert.Empty(await store.GetStepsAsync(recipeId, tenantId: null, ct));
        Assert.Empty(await store.ListAttachmentsAsync(RecipeEntityTypes.Recipe, recipeId, tenantId: null, ct));
    }

    [Fact]
    public async Task DeleteIngredient_WhenReferenced_ThrowsInvalidOperation()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IRecipeStore>();
        var ingredient = new IngredientRecord { Name = "Butter", Slug = Unique("butter") };
        await store.SaveIngredientAsync(ingredient, tenantId: null, ct);
        var ingredientId = (await store.GetIngredientBySlugAsync(ingredient.Slug, tenantId: null, ct))!.Id;
        var recipeId = LyoGuid.CreateV7();
        await store.SaveRecipeAsync(new RecipeRecord { Id = recipeId, Title = "Cookies", Slug = Unique("cookies") }, tenantId: null, ct);
        await store.ReplaceIngredientLinesAsync(
            recipeId, [new() { RecipeId = recipeId, IngredientId = ingredientId, SortOrder = 0 }], tenantId: null, ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.DeleteIngredientAsync(ingredientId, tenantId: null, ct));
    }

    [Fact]
    public async Task DeleteCategory_WhenItHasRecipes_ThrowsInvalidOperation()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IRecipeStore>();
        var category = new RecipeCategoryRecord { Name = "Mains", Slug = Unique("mains") };
        await store.SaveCategoryAsync(category, tenantId: null, ct);
        var categoryId = (await store.ListCategoriesAsync(null, ct)).Single(c => c.Slug == category.Slug).Id;
        await store.SaveRecipeAsync(
            new RecipeRecord { Id = LyoGuid.CreateV7(), Title = "Stew", Slug = Unique("stew"), CategoryId = categoryId },
            tenantId: null, ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.DeleteCategoryAsync(categoryId, tenantId: null, ct));
    }

    [Fact]
    public async Task HealthCheck_Succeeds()
    {
        var ct = TestCt;
        var health = (Lyo.Health.IHealth)fixture.ServiceProvider.GetRequiredService<IRecipeStore>();
        var result = await health.CheckHealthAsync(ct);
        Assert.True(result.IsHealthy);
        Assert.Equal("recipe-postgres", health.HealthCheckName);
    }

    [Fact]
    public async Task ReplaceIngredientLines_LineOnTwoSteps_RoundTripsStepIds()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IRecipeStore>();
        var recipeId = LyoGuid.CreateV7();
        await store.SaveRecipeAsync(new RecipeRecord { Id = recipeId, Title = "Soup", Slug = Unique("soup-steps") }, tenantId: null, ct);
        var mix = LyoGuid.CreateV7();
        var simmer = LyoGuid.CreateV7();
        await store.ReplaceStepsAsync(
            recipeId,
            [
                new() { Id = mix, RecipeId = recipeId, SortOrder = 0, Instruction = "Mix" },
                new() { Id = simmer, RecipeId = recipeId, SortOrder = 1, Instruction = "Simmer" }
            ], tenantId: null, ct);
        var lineId = LyoGuid.CreateV7();
        await store.ReplaceIngredientLinesAsync(
            recipeId,
            [new() { Id = lineId, RecipeId = recipeId, IngredientText = "salt", SortOrder = 0, StepIds = [mix, simmer] }],
            tenantId: null, ct);
        var line = Assert.Single(await store.GetIngredientLinesAsync(recipeId, tenantId: null, ct));
        Assert.Equal(lineId, line.Id);
        Assert.Contains(mix, line.StepIds);
        Assert.Contains(simmer, line.StepIds);
    }

    [Fact]
    public async Task ReplaceIngredientLines_StepFromAnotherRecipe_Throws()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IRecipeStore>();
        var otherId = LyoGuid.CreateV7();
        var recipeId = LyoGuid.CreateV7();
        await store.SaveRecipeAsync(new RecipeRecord { Id = otherId, Title = "Other", Slug = Unique("other") }, tenantId: null, ct);
        await store.SaveRecipeAsync(new RecipeRecord { Id = recipeId, Title = "This", Slug = Unique("this") }, tenantId: null, ct);
        var foreignStep = LyoGuid.CreateV7();
        await store.ReplaceStepsAsync(
            otherId, [new() { Id = foreignStep, RecipeId = otherId, SortOrder = 0, Instruction = "Wait" }], tenantId: null, ct);
        await Assert.ThrowsAsync<ArgumentException>(() => store.ReplaceIngredientLinesAsync(
            recipeId,
            [new() { RecipeId = recipeId, IngredientText = "salt", SortOrder = 0, StepIds = [foreignStep] }],
            tenantId: null, ct));
    }

    [Fact]
    public async Task ReplaceSteps_KeptId_PreservesRow()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IRecipeStore>();
        var recipeId = LyoGuid.CreateV7();
        await store.SaveRecipeAsync(new RecipeRecord { Id = recipeId, Title = "Eggs", Slug = Unique("eggs-keep") }, tenantId: null, ct);
        var keep = LyoGuid.CreateV7();
        var drop = LyoGuid.CreateV7();
        await store.ReplaceStepsAsync(
            recipeId,
            [
                new() { Id = keep, RecipeId = recipeId, SortOrder = 0, Instruction = "Crack" },
                new() { Id = drop, RecipeId = recipeId, SortOrder = 1, Instruction = "Cook" }
            ], tenantId: null, ct);
        await store.ReplaceStepsAsync(
            recipeId, [new() { Id = keep, RecipeId = recipeId, SortOrder = 0, Instruction = "Whisk" }], tenantId: null, ct);
        var step = Assert.Single(await store.GetStepsAsync(recipeId, tenantId: null, ct));
        Assert.Equal(keep, step.Id);
        Assert.Equal("Whisk", step.Instruction);
    }

    [Fact]
    public async Task ReplaceSteps_RemovedStep_DeletesItsAttachmentsAndLineLinks()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IRecipeStore>();
        var recipeId = LyoGuid.CreateV7();
        await store.SaveRecipeAsync(new RecipeRecord { Id = recipeId, Title = "Sauce", Slug = Unique("sauce") }, tenantId: null, ct);
        var keep = LyoGuid.CreateV7();
        var drop = LyoGuid.CreateV7();
        await store.ReplaceStepsAsync(
            recipeId,
            [
                new() { Id = keep, RecipeId = recipeId, SortOrder = 0, Instruction = "Chop" },
                new() { Id = drop, RecipeId = recipeId, SortOrder = 1, Instruction = "Simmer" }
            ], tenantId: null, ct);
        await store.SaveAttachmentAsync(
            new RecipeAttachmentRecord {
                EntityType = RecipeEntityTypes.RecipeStep,
                EntityId = drop,
                FileId = LyoGuid.CreateV7(),
                Role = RecipeAttachmentRole.Image
            }, tenantId: null, ct);
        await store.ReplaceIngredientLinesAsync(
            recipeId,
            [new() { RecipeId = recipeId, IngredientText = "tomato", SortOrder = 0, StepIds = [keep, drop] }],
            tenantId: null, ct);
        await store.ReplaceStepsAsync(
            recipeId, [new() { Id = keep, RecipeId = recipeId, SortOrder = 0, Instruction = "Chop" }], tenantId: null, ct);
        Assert.Empty(await store.ListAttachmentsAsync(RecipeEntityTypes.RecipeStep, drop, tenantId: null, ct));
        var line = Assert.Single(await store.GetIngredientLinesAsync(recipeId, tenantId: null, ct));
        Assert.Contains(keep, line.StepIds);
        Assert.DoesNotContain(drop, line.StepIds);
    }

    [Fact]
    public async Task DeleteRecipe_WithStepImages_RemovesStepAttachments()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IRecipeStore>();
        var recipeId = LyoGuid.CreateV7();
        var stepId = LyoGuid.CreateV7();
        await store.SaveRecipeAsync(new RecipeRecord { Id = recipeId, Title = "Roast", Slug = Unique("roast") }, tenantId: null, ct);
        await store.ReplaceStepsAsync(
            recipeId, [new() { Id = stepId, RecipeId = recipeId, SortOrder = 0, Instruction = "Roast" }], tenantId: null, ct);
        await store.SaveAttachmentAsync(
            new RecipeAttachmentRecord {
                EntityType = RecipeEntityTypes.RecipeStep,
                EntityId = stepId,
                FileId = LyoGuid.CreateV7(),
                Role = RecipeAttachmentRole.Image
            }, tenantId: null, ct);
        await store.DeleteRecipeAsync(recipeId, tenantId: null, ct);
        Assert.Empty(await store.ListAttachmentsAsync(RecipeEntityTypes.RecipeStep, stepId, tenantId: null, ct));
    }

    [Fact]
    public async Task SaveUnit_ThenList_ReturnsAbbreviation()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IRecipeStore>();
        var abbreviation = Unique("tsp");
        await store.SaveUnitAsync(new UnitRecord { Name = "Teaspoon", Abbreviation = abbreviation, SortOrder = 0 }, tenantId: null, ct);
        var units = await store.ListUnitsAsync(tenantId: null, ct);
        Assert.Contains(units, u => u.Abbreviation == abbreviation && u.Name == "Teaspoon");
        var recipeId = LyoGuid.CreateV7();
        await store.SaveRecipeAsync(new RecipeRecord { Id = recipeId, Title = "Pinch", Slug = Unique("pinch") }, tenantId: null, ct);
        await store.ReplaceIngredientLinesAsync(
            recipeId, [new() { RecipeId = recipeId, IngredientText = "salt", SortOrder = 0, Unit = "pinch" }], tenantId: null, ct);
        var line = Assert.Single(await store.GetIngredientLinesAsync(recipeId, tenantId: null, ct));
        Assert.Equal("pinch", line.Unit);
    }

    [Fact]
    public async Task ListIngredientUsage_LineOnTwoSteps_ReturnsRecipeAndBothSteps()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IRecipeStore>();
        var ingredient = new IngredientRecord { Name = "Salt", Slug = Unique("salt-use") };
        await store.SaveIngredientAsync(ingredient, tenantId: null, ct);
        var ingredientId = (await store.GetIngredientBySlugAsync(ingredient.Slug, tenantId: null, ct))!.Id;
        var recipeId = LyoGuid.CreateV7();
        await store.SaveRecipeAsync(new RecipeRecord { Id = recipeId, Title = "Soup", Slug = Unique("soup-use") }, tenantId: null, ct);
        var mix = LyoGuid.CreateV7();
        var finish = LyoGuid.CreateV7();
        await store.ReplaceStepsAsync(
            recipeId,
            [
                new() { Id = mix, RecipeId = recipeId, SortOrder = 0, Instruction = "Mix" },
                new() { Id = finish, RecipeId = recipeId, SortOrder = 1, Instruction = "Finish" }
            ], tenantId: null, ct);
        await store.ReplaceIngredientLinesAsync(
            recipeId,
            [new() { RecipeId = recipeId, IngredientId = ingredientId, SortOrder = 0, StepIds = [mix, finish] }],
            tenantId: null, ct);
        var usage = Assert.Single(await store.ListIngredientUsageAsync(ingredientId, tenantId: null, ct));
        Assert.Equal(recipeId, usage.RecipeId);
        Assert.Equal("Soup", usage.RecipeTitle);
        Assert.Contains(mix, usage.StepIds);
        Assert.Contains(finish, usage.StepIds);
    }

    private static string Unique(string prefix) => prefix + "-" + LyoGuid.CreateV7().ToString("N")[..8];
}
