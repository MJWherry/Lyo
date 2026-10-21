using Lyo.Common.Core.Identifiers;
using Lyo.Recipe.Models;
using Lyo.Recipe.Models.Enums;
using Lyo.Web.Primitives;
using MudBlazor;

namespace Lyo.Recipe.Web.Components.Tests;

public sealed class RecipeColorHelperTests
{
    [Fact]
    public void RecipeColorHelper_StatusAndDifficulty_MapToExpectedColors()
    {
        Assert.Equal(Color.Success, RecipeColorHelper.ForStatus(RecipeStatus.Active));
        Assert.Equal(Color.Default, RecipeColorHelper.ForStatus(RecipeStatus.Draft));
        Assert.Equal(Color.Warning, RecipeColorHelper.ForStatus(RecipeStatus.Archived));
        Assert.Equal(Color.Success, RecipeColorHelper.ForStatus("Active"));
        Assert.Equal(Color.Success, RecipeColorHelper.ForDifficulty(RecipeDifficulty.Easy));
        Assert.Equal(Color.Warning, RecipeColorHelper.ForDifficulty(RecipeDifficulty.Medium));
        Assert.Equal(Color.Error, RecipeColorHelper.ForDifficulty(RecipeDifficulty.Hard));
        Assert.Equal(LyoChipHue.Lime, RecipeColorHelper.ForRole(RecipeAttachmentRole.Scan));
        Assert.Equal(LyoChipHue.Indigo, RecipeColorHelper.ForRole(RecipeAttachmentRole.Document));
        Assert.Equal(LyoChipHue.Amber, RecipeColorHelper.ForRole(RecipeAttachmentRole.Card));
        Assert.Equal(LyoChipHue.Rose, RecipeColorHelper.ForRole(RecipeAttachmentRole.Image));
        Assert.Null(RecipeColorHelper.ForRole(RecipeAttachmentRole.Other));
    }
}

public sealed class RecipeFilePathsTests
{
    [Fact]
    public void RecipeFilePaths_ForRecipe_UsesStablePrefix()
    {
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Assert.Equal("recipe/recipes/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", RecipeFilePaths.ForRecipe(id));
        Assert.Equal(RecipeFilePaths.ForRecipe(id), RecipeFilePaths.ForEntity(RecipeEntityTypes.Recipe, id));
    }

    [Fact]
    public void ForEntity_Unknown_Throws() => Assert.Throws<ArgumentException>(() => RecipeFilePaths.ForEntity("Nope", LyoGuid.CreateV7()));

    [Fact]
    public void ForEntity_RecipeStep_ReturnsStepPrefix()
    {
        var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Assert.Equal("recipe/steps/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", RecipeFilePaths.ForStep(id));
        Assert.Equal(RecipeFilePaths.ForStep(id), RecipeFilePaths.ForEntity(RecipeEntityTypes.RecipeStep, id));
    }
}

public sealed class RecipeQuantityScaleTests
{
    [Fact]
    public void Scale_DoubleServings_DoublesQuantity()
    {
        Assert.Equal(4m, RecipeQuantityScale.Scale(2m, 4, 8));
        Assert.Null(RecipeQuantityScale.Scale(null, 4, 8));
    }
}