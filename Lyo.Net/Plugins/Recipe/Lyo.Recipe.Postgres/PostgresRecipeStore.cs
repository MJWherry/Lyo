using Lyo.Common.Core.Identifiers;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Postgres;
using Lyo.Recipe.Models;
using Lyo.Recipe.Models.Enums;
using Lyo.Recipe.Postgres.Database;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Recipe.Postgres;

/// <summary>Postgres-backed <see cref="IRecipeStore" />.</summary>
public sealed class PostgresRecipeStore : IRecipeStore, IHealth
{
    private readonly IDbContextFactory<RecipeDbContext> _contextFactory;
    private readonly EntityRefOptions _entityRefOptions;
    private readonly TenancyOptions _featureTenancy;

    public PostgresRecipeStore(IDbContextFactory<RecipeDbContext> contextFactory, EntityRefOptions entityRefOptions, PostgresRecipeOptions recipeOptions)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        ArgumentHelpers.ThrowIfNull(entityRefOptions);
        ArgumentHelpers.ThrowIfNull(recipeOptions);
        _contextFactory = contextFactory;
        _entityRefOptions = entityRefOptions;
        _featureTenancy = recipeOptions.Tenancy;
    }

    /// <inheritdoc />
    public string HealthCheckName => "recipe-postgres";

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default) => PostgresHealth.CheckAsync(_contextFactory, PostgresRecipeOptions.Schema, ct);

    /// <inheritdoc />
    public async Task SaveCategoryAsync(RecipeCategoryRecord category, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(category);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(category.Name, nameof(category.Name));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(category.Slug, nameof(category.Slug));
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (category.Id != Guid.Empty) {
            var existing = await context.Categories.FirstOrDefaultAsync(c => c.Id == category.Id && c.TenantId == resolvedTenant, ct).ConfigureAwait(false);
            if (existing != null) {
                existing.ParentId = category.ParentId;
                existing.Name = category.Name;
                existing.Slug = category.Slug;
                existing.TenantId = resolvedTenant;
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                return;
            }
        }

        context.Categories.Add(
            new() {
                Id = category.Id == Guid.Empty ? LyoGuid.CreateV7() : category.Id,
                ParentId = category.ParentId,
                Name = category.Name,
                Slug = category.Slug,
                TenantId = resolvedTenant,
                CreatedTimestamp = category.CreatedTimestamp == default ? DateTime.UtcNow : category.CreatedTimestamp
            });

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RecipeCategoryRecord?> GetCategoryByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var e = await context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        return e == null ? null : ToCategoryRecord(e);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecipeCategoryRecord>> ListCategoriesAsync(Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var list = await context.Categories.AsNoTracking().Where(c => c.TenantId == resolvedTenant).OrderBy(c => c.Name).ToListAsync(ct).ConfigureAwait(false);
        return list.Select(ToCategoryRecord).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteCategoryAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        OperationHelpers.ThrowIf(
            await context.Categories.AnyAsync(c => c.ParentId == id && c.TenantId == resolvedTenant, ct).ConfigureAwait(false), "Reassign or delete child categories first.");

        OperationHelpers.ThrowIf(
            await context.Recipes.AnyAsync(r => r.CategoryId == id && r.TenantId == resolvedTenant, ct).ConfigureAwait(false), "Category is still assigned to recipes.");

        var entity = await context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        if (entity != null) {
            await RemoveAttachmentsAsync(context, RecipeEntityTypes.RecipeCategory, id, resolvedTenant, ct).ConfigureAwait(false);
            context.Categories.Remove(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task SaveIngredientAsync(IngredientRecord ingredient, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(ingredient);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(ingredient.Name, nameof(ingredient.Name));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(ingredient.Slug, nameof(ingredient.Slug));
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (ingredient.Id != Guid.Empty) {
            var existing = await context.Ingredients.FirstOrDefaultAsync(i => i.Id == ingredient.Id && i.TenantId == resolvedTenant, ct).ConfigureAwait(false);
            if (existing != null) {
                existing.Name = ingredient.Name;
                existing.Slug = ingredient.Slug;
                existing.DefaultUnit = ingredient.DefaultUnit;
                existing.Notes = ingredient.Notes;
                existing.TenantId = resolvedTenant;
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                return;
            }
        }

        context.Ingredients.Add(
            new() {
                Id = ingredient.Id == Guid.Empty ? LyoGuid.CreateV7() : ingredient.Id,
                Name = ingredient.Name,
                Slug = ingredient.Slug,
                DefaultUnit = ingredient.DefaultUnit,
                Notes = ingredient.Notes,
                TenantId = resolvedTenant,
                CreatedTimestamp = ingredient.CreatedTimestamp == default ? DateTime.UtcNow : ingredient.CreatedTimestamp
            });

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IngredientRecord?> GetIngredientByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var e = await context.Ingredients.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id && i.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        return e == null ? null : ToIngredientRecord(e);
    }

    /// <inheritdoc />
    public async Task<IngredientRecord?> GetIngredientBySlugAsync(string slug, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(slug);
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var e = await context.Ingredients.AsNoTracking().FirstOrDefaultAsync(i => i.Slug == slug && i.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        return e == null ? null : ToIngredientRecord(e);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IngredientRecord>> ListIngredientsAsync(Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var list = await context.Ingredients.AsNoTracking().Where(i => i.TenantId == resolvedTenant).OrderBy(i => i.Name).ToListAsync(ct).ConfigureAwait(false);
        return list.Select(ToIngredientRecord).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteIngredientAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        OperationHelpers.ThrowIf(
            await context.IngredientLines.AnyAsync(l => l.IngredientId == id && l.TenantId == resolvedTenant, ct).ConfigureAwait(false),
            "Ingredient is still used on a recipe line.");

        var entity = await context.Ingredients.FirstOrDefaultAsync(i => i.Id == id && i.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        if (entity != null) {
            await RemoveAttachmentsAsync(context, RecipeEntityTypes.Ingredient, id, resolvedTenant, ct).ConfigureAwait(false);
            context.Ingredients.Remove(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task SaveRecipeAsync(RecipeRecord recipe, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(recipe);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(recipe.Title, nameof(recipe.Title));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(recipe.Slug, nameof(recipe.Slug));
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (recipe.Id != Guid.Empty) {
            var existing = await context.Recipes.FirstOrDefaultAsync(r => r.Id == recipe.Id && r.TenantId == resolvedTenant, ct).ConfigureAwait(false);
            if (existing != null) {
                CopyToEntity(recipe, existing);
                existing.TenantId = resolvedTenant;
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                return;
            }
        }

        var entity = new RecipeEntity();
        CopyToEntity(recipe, entity);
        entity.Id = recipe.Id == Guid.Empty ? LyoGuid.CreateV7() : recipe.Id;
        entity.TenantId = resolvedTenant;
        if (entity.CreatedTimestamp == default)
            entity.CreatedTimestamp = DateTime.UtcNow;

        context.Recipes.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RecipeRecord?> GetRecipeByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var e = await context.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id && r.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        return e == null ? null : ToRecipeRecord(e);
    }

    /// <inheritdoc />
    public async Task<RecipeRecord?> GetRecipeBySlugAsync(string slug, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(slug);
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var e = await context.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.Slug == slug && r.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        return e == null ? null : ToRecipeRecord(e);
    }

    /// <inheritdoc />
    public async Task DeleteRecipeAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Recipes.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        if (entity != null) {
            var stepIds = await context.Steps.Where(s => s.RecipeId == id && s.TenantId == resolvedTenant).Select(s => s.Id).ToListAsync(ct).ConfigureAwait(false);
            var lineIds = await context.IngredientLines.Where(l => l.RecipeId == id && l.TenantId == resolvedTenant).Select(l => l.Id).ToListAsync(ct).ConfigureAwait(false);
            await RemoveLineLinksAsync(context, lineIds, stepIds, ct).ConfigureAwait(false);
            foreach (var stepId in stepIds)
                await RemoveAttachmentsAsync(context, RecipeEntityTypes.RecipeStep, stepId, resolvedTenant, ct).ConfigureAwait(false);

            await RemoveAttachmentsAsync(context, RecipeEntityTypes.Recipe, id, resolvedTenant, ct).ConfigureAwait(false);
            context.Recipes.Remove(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecipeIngredientLineRecord>> GetIngredientLinesAsync(Guid recipeId, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var list = await context.IngredientLines.AsNoTracking()
            .Where(l => l.RecipeId == recipeId && l.TenantId == resolvedTenant)
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var lineIds = list.Select(l => l.Id).ToList();
        var links = lineIds.Count == 0 ? [] : await context.IngredientLineSteps.AsNoTracking().Where(j => lineIds.Contains(j.LineId)).ToListAsync(ct).ConfigureAwait(false);
        return list.Select(e => {
                var record = ToLineRecord(e);
                record.StepIds = links.Where(j => j.LineId == e.Id).Select(j => j.StepId).ToList();
                return record;
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task ReplaceIngredientLinesAsync(Guid recipeId, IReadOnlyList<RecipeIngredientLineRecord> lines, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(lines);
        foreach (var line in lines) {
            ArgumentHelpers.ThrowIfNull(line);
            if (line.IngredientId is null && string.IsNullOrWhiteSpace(line.IngredientText))
                throw new ArgumentException("An ingredient line requires IngredientId or IngredientText.", nameof(lines));
        }

        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var requestedStepIds = lines.SelectMany(l => l.StepIds ?? []).Distinct().ToList();
        if (requestedStepIds.Count > 0) {
            var owned = await context.Steps.AsNoTracking()
                .Where(s => s.RecipeId == recipeId && s.TenantId == resolvedTenant && requestedStepIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (owned.Count != requestedStepIds.Count)
                throw new ArgumentException("Ingredient line steps must belong to the same recipe.", nameof(lines));
        }

        await using var tx = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        var existing = await context.IngredientLines.Where(l => l.RecipeId == recipeId && l.TenantId == resolvedTenant).ToListAsync(ct).ConfigureAwait(false);
        var incomingIds = lines.Where(l => l.Id != Guid.Empty).Select(l => l.Id).ToHashSet();
        var removed = existing.Where(e => !incomingIds.Contains(e.Id)).ToList();
        if (removed.Count > 0) {
            await RemoveLineLinksAsync(context, removed.Select(e => e.Id).ToList(), [], ct).ConfigureAwait(false);
            context.IngredientLines.RemoveRange(removed);
        }

        var kept = new List<(RecipeIngredientLineEntity Entity, List<Guid> StepIds)>();
        foreach (var line in lines) {
            var id = line.Id == Guid.Empty ? LyoGuid.CreateV7() : line.Id;
            var entity = existing.FirstOrDefault(e => e.Id == id);
            if (entity == null) {
                entity = new() { Id = id, RecipeId = recipeId, TenantId = resolvedTenant };
                context.IngredientLines.Add(entity);
            }

            entity.IngredientId = line.IngredientId;
            entity.IngredientText = line.IngredientText;
            entity.SortOrder = line.SortOrder;
            entity.Quantity = line.Quantity;
            entity.Unit = line.Unit;
            entity.Preparation = line.Preparation;
            entity.IsOptional = line.IsOptional;
            entity.GroupLabel = line.GroupLabel;
            entity.TenantId = resolvedTenant;
            kept.Add((entity, (line.StepIds ?? []).Distinct().ToList()));
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        var keptIds = kept.Select(k => k.Entity.Id).ToList();
        await RemoveLineLinksAsync(context, keptIds, [], ct).ConfigureAwait(false);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        foreach (var (entity, stepIds) in kept) {
            foreach (var stepId in stepIds)
                context.IngredientLineSteps.Add(new() { LineId = entity.Id, StepId = stepId });
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        await tx.CommitAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecipeStepRecord>> GetStepsAsync(Guid recipeId, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var list = await context.Steps.AsNoTracking()
            .Where(s => s.RecipeId == recipeId && s.TenantId == resolvedTenant)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return list.Select(ToStepRecord).ToList();
    }

    /// <inheritdoc />
    public async Task ReplaceStepsAsync(Guid recipeId, IReadOnlyList<RecipeStepRecord> steps, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(steps);
        foreach (var step in steps) {
            ArgumentHelpers.ThrowIfNull(step);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(step.Instruction, nameof(step.Instruction));
        }

        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var tx = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        var existing = await context.Steps.Where(s => s.RecipeId == recipeId && s.TenantId == resolvedTenant).ToListAsync(ct).ConfigureAwait(false);
        var incomingIds = steps.Where(s => s.Id != Guid.Empty).Select(s => s.Id).ToHashSet();
        var removed = existing.Where(e => !incomingIds.Contains(e.Id)).ToList();
        if (removed.Count > 0) {
            var removedIds = removed.Select(s => s.Id).ToList();
            await RemoveLineLinksAsync(context, [], removedIds, ct).ConfigureAwait(false);
            foreach (var stepId in removedIds)
                await RemoveAttachmentsAsync(context, RecipeEntityTypes.RecipeStep, stepId, resolvedTenant, ct).ConfigureAwait(false);

            context.Steps.RemoveRange(removed);
        }

        foreach (var step in steps) {
            var id = step.Id == Guid.Empty ? LyoGuid.CreateV7() : step.Id;
            var entity = existing.FirstOrDefault(e => e.Id == id);
            if (entity == null) {
                entity = new() { Id = id, RecipeId = recipeId, TenantId = resolvedTenant };
                context.Steps.Add(entity);
            }

            entity.SortOrder = step.SortOrder;
            entity.Instruction = step.Instruction;
            entity.DurationMinutes = step.DurationMinutes;
            entity.GroupLabel = step.GroupLabel;
            entity.TenantId = resolvedTenant;
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        await tx.CommitAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IngredientUsageRecord>> ListIngredientUsageAsync(Guid ingredientId, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var lines = await context.IngredientLines.AsNoTracking().Where(l => l.IngredientId == ingredientId && l.TenantId == resolvedTenant).ToListAsync(ct).ConfigureAwait(false);
        if (lines.Count == 0)
            return [];

        var lineIds = lines.Select(l => l.Id).ToList();
        var links = await context.IngredientLineSteps.AsNoTracking().Where(j => lineIds.Contains(j.LineId)).ToListAsync(ct).ConfigureAwait(false);
        var stepIds = links.Select(j => j.StepId).Distinct().ToList();
        var steps = stepIds.Count == 0
            ? []
            : await context.Steps.AsNoTracking().Where(s => stepIds.Contains(s.Id)).Select(s => new { s.Id, s.SortOrder }).ToListAsync(ct).ConfigureAwait(false);

        var recipeIds = lines.Select(l => l.RecipeId).Distinct().ToList();
        var recipes = await context.Recipes.AsNoTracking().Where(r => recipeIds.Contains(r.Id)).Select(r => new { r.Id, r.Title }).ToListAsync(ct).ConfigureAwait(false);
        return lines.Select(line => {
                var ordered = links.Where(j => j.LineId == line.Id)
                    .Select(j => steps.FirstOrDefault(s => s.Id == j.StepId))
                    .Where(s => s != null)
                    .OrderBy(s => s!.SortOrder)
                    .ThenBy(s => s!.Id)
                    .ToList();

                return new IngredientUsageRecord {
                    RecipeId = line.RecipeId,
                    RecipeTitle = recipes.FirstOrDefault(r => r.Id == line.RecipeId)?.Title ?? string.Empty,
                    LineId = line.Id,
                    StepIds = ordered.Select(s => s!.Id).ToList(),
                    StepNumbers = ordered.Select(s => s!.SortOrder + 1).ToList()
                };
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task SaveUnitAsync(UnitRecord unit, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(unit);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(unit.Name, nameof(unit.Name));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(unit.Abbreviation, nameof(unit.Abbreviation));
        unit.Name = unit.Name.Trim();
        unit.Abbreviation = unit.Abbreviation.Trim();
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (unit.Id == Guid.Empty)
            unit.Id = LyoGuid.CreateV7();

        var duplicate = await context.Units.AnyAsync(u => u.Id != unit.Id && u.TenantId == resolvedTenant && u.Abbreviation == unit.Abbreviation, ct).ConfigureAwait(false);
        if (duplicate)
            throw new InvalidOperationException("A unit with that abbreviation already exists.");

        var existing = await context.Units.FirstOrDefaultAsync(u => u.Id == unit.Id && u.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        if (existing != null) {
            existing.Name = unit.Name;
            existing.Abbreviation = unit.Abbreviation;
            existing.SortOrder = unit.SortOrder;
            existing.TenantId = resolvedTenant;
        }
        else {
            context.Units.Add(
                new() {
                    Id = unit.Id,
                    Name = unit.Name,
                    Abbreviation = unit.Abbreviation,
                    SortOrder = unit.SortOrder,
                    TenantId = resolvedTenant
                });
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UnitRecord>> ListUnitsAsync(Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var list = await context.Units.AsNoTracking()
            .Where(u => u.TenantId == resolvedTenant)
            .OrderBy(u => u.SortOrder)
            .ThenBy(u => u.Abbreviation)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return list.Select(u => new UnitRecord {
                Id = u.Id,
                Name = u.Name,
                Abbreviation = u.Abbreviation,
                SortOrder = u.SortOrder
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task DeleteUnitAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Units.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        if (entity != null) {
            context.Units.Remove(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task SaveAttachmentAsync(RecipeAttachmentRecord attachment, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(attachment);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(attachment.EntityType, nameof(attachment.EntityType));
        OperationHelpers.ThrowIf(attachment.EntityId == Guid.Empty, "Attachment entity id is required.");
        OperationHelpers.ThrowIf(attachment.FileId == Guid.Empty, "Attachment file id is required.");
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (attachment.Id != Guid.Empty) {
            var existing = await context.Attachments.FirstOrDefaultAsync(a => a.Id == attachment.Id && a.TenantId == resolvedTenant, ct).ConfigureAwait(false);
            if (existing != null) {
                CopyToEntity(attachment, existing);
                existing.TenantId = resolvedTenant;
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                return;
            }
        }

        var entity = new RecipeAttachmentEntity();
        CopyToEntity(attachment, entity);
        entity.Id = attachment.Id == Guid.Empty ? LyoGuid.CreateV7() : attachment.Id;
        entity.TenantId = resolvedTenant;
        if (entity.CreatedTimestamp == default)
            entity.CreatedTimestamp = DateTime.UtcNow;

        context.Attachments.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RecipeAttachmentRecord?> GetAttachmentByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Attachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id && a.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        return entity == null ? null : ToAttachmentRecord(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecipeAttachmentRecord>> ListAttachmentsAsync(string entityType, Guid entityId, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(entityType);
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var list = await context.Attachments.AsNoTracking()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId && a.TenantId == resolvedTenant)
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.CreatedTimestamp)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return list.Select(ToAttachmentRecord).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteAttachmentAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Attachments.FirstOrDefaultAsync(a => a.Id == id && a.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        if (entity != null) {
            context.Attachments.Remove(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    private Guid? ResolveTenant(Guid? tenantId) => TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);

    private static async Task RemoveLineLinksAsync(RecipeDbContext context, IReadOnlyCollection<Guid> lineIds, IReadOnlyCollection<Guid> stepIds, CancellationToken ct)
    {
        if (lineIds.Count == 0 && stepIds.Count == 0)
            return;

        var query = context.IngredientLineSteps.AsQueryable();
        if (lineIds.Count > 0 && stepIds.Count > 0)
            query = query.Where(j => lineIds.Contains(j.LineId) || stepIds.Contains(j.StepId));
        else if (lineIds.Count > 0)
            query = query.Where(j => lineIds.Contains(j.LineId));
        else
            query = query.Where(j => stepIds.Contains(j.StepId));

        var rows = await query.ToListAsync(ct).ConfigureAwait(false);
        if (rows.Count > 0)
            context.IngredientLineSteps.RemoveRange(rows);
    }

    private static async Task RemoveAttachmentsAsync(RecipeDbContext context, string entityType, Guid entityId, Guid? tenantId, CancellationToken ct)
    {
        var rows = await context.Attachments.Where(a => a.EntityType == entityType && a.EntityId == entityId && a.TenantId == tenantId).ToListAsync(ct).ConfigureAwait(false);
        if (rows.Count > 0)
            context.Attachments.RemoveRange(rows);
    }

    private static void CopyToEntity(RecipeRecord recipe, RecipeEntity e)
    {
        e.Title = recipe.Title;
        e.Slug = recipe.Slug;
        e.Description = recipe.Description;
        e.Servings = recipe.Servings;
        e.PrepMinutes = recipe.PrepMinutes;
        e.CookMinutes = recipe.CookMinutes;
        e.Difficulty = (int)recipe.Difficulty;
        e.Status = (int)recipe.Status;
        e.SourceName = recipe.SourceName;
        e.SourceUrl = recipe.SourceUrl;
        e.YieldLabel = recipe.YieldLabel;
        e.CategoryId = recipe.CategoryId;
    }

    private static void CopyToEntity(RecipeAttachmentRecord attachment, RecipeAttachmentEntity e)
    {
        e.EntityType = attachment.EntityType;
        e.EntityId = attachment.EntityId;
        e.FileId = attachment.FileId;
        e.Role = (int)attachment.Role;
        e.Caption = attachment.Caption;
        e.IsPrimary = attachment.IsPrimary;
        e.SortOrder = attachment.SortOrder;
    }

    private static RecipeCategoryRecord ToCategoryRecord(RecipeCategoryEntity e)
        => new() {
            Id = e.Id,
            ParentId = e.ParentId,
            Name = e.Name,
            Slug = e.Slug,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp
        };

    private static IngredientRecord ToIngredientRecord(IngredientEntity e)
        => new() {
            Id = e.Id,
            Name = e.Name,
            Slug = e.Slug,
            DefaultUnit = e.DefaultUnit,
            Notes = e.Notes,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp
        };

    private static RecipeRecord ToRecipeRecord(RecipeEntity e)
        => new() {
            Id = e.Id,
            Title = e.Title,
            Slug = e.Slug,
            Description = e.Description,
            Servings = e.Servings,
            PrepMinutes = e.PrepMinutes,
            CookMinutes = e.CookMinutes,
            Difficulty = (RecipeDifficulty)e.Difficulty,
            Status = (RecipeStatus)e.Status,
            SourceName = e.SourceName,
            SourceUrl = e.SourceUrl,
            YieldLabel = e.YieldLabel,
            CategoryId = e.CategoryId,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp
        };

    private static RecipeIngredientLineRecord ToLineRecord(RecipeIngredientLineEntity e)
        => new() {
            Id = e.Id,
            RecipeId = e.RecipeId,
            IngredientId = e.IngredientId,
            IngredientText = e.IngredientText,
            SortOrder = e.SortOrder,
            Quantity = e.Quantity,
            Unit = e.Unit,
            Preparation = e.Preparation,
            IsOptional = e.IsOptional,
            GroupLabel = e.GroupLabel
        };

    private static RecipeStepRecord ToStepRecord(RecipeStepEntity e)
        => new() {
            Id = e.Id,
            RecipeId = e.RecipeId,
            SortOrder = e.SortOrder,
            Instruction = e.Instruction,
            DurationMinutes = e.DurationMinutes,
            GroupLabel = e.GroupLabel
        };

    private static RecipeAttachmentRecord ToAttachmentRecord(RecipeAttachmentEntity e)
        => new() {
            Id = e.Id,
            EntityType = e.EntityType,
            EntityId = e.EntityId,
            FileId = e.FileId,
            Role = (RecipeAttachmentRole)e.Role,
            Caption = e.Caption,
            IsPrimary = e.IsPrimary,
            SortOrder = e.SortOrder,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp
        };
}