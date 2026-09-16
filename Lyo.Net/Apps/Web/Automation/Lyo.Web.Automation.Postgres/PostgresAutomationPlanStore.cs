using System.Text.Json;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Cache;
using Lyo.Common.Core.Identifiers;
using Lyo.Json;
using Lyo.Exceptions;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Plan;
using Lyo.Web.Automation.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Web.Automation.Postgres;

/// <summary>Postgres-backed <see cref="IAutomationPlanStore" />.</summary>
public sealed class PostgresAutomationPlanStore(IDbContextFactory<AutomationPlanDbContext> contexts, IServiceProvider services) : IAutomationPlanStore
{
    private static readonly JsonSerializerOptions Json = LyoJsonSerializerOptions.Create(options => options.AllowOutOfOrderMetadataProperties = true);

    /// <inheritdoc />
    public IReadOnlyList<AutomationPlanSummary> List()
    {
        using var db = contexts.CreateDbContext();
        return db.Plans.AsNoTracking().OrderBy(plan => plan.Name).Select(plan => new AutomationPlanSummary { Id = plan.Id, Name = plan.Name }).ToArray();
    }

    /// <inheritdoc />
    public AutomationPlan? GetByName(string name)
    {
        using var db = contexts.CreateDbContext();
        var row = db.Plans.AsNoTracking().FirstOrDefault(plan => plan.Name == name);
        return row is null ? null : Read(row.Document);
    }

    /// <inheritdoc />
    public AutomationPlan? GetById(Guid id)
    {
        using var db = contexts.CreateDbContext();
        var row = db.Plans.AsNoTracking().FirstOrDefault(plan => plan.Id == id);
        return row is null ? null : Read(row.Document);
    }

    /// <inheritdoc />
    public AutomationPlan Create(string name)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        using var db = contexts.CreateDbContext();
        var existing = db.Plans.FirstOrDefault(plan => plan.Name == name);
        if (existing != null)
            return Read(existing.Document)!;

        var plan = new AutomationPlan(name, []);
        db.Plans.Add(new AutomationPlanEntity {
            Id = LyoGuid.CreateV7(),
            Name = name,
            Document = JsonSerializer.Serialize(plan, Json),
            StepCount = 0,
            UpdatedUtc = DateTime.UtcNow
        });
        db.SaveChanges();
        InvalidateQueries();
        return plan;
    }

    /// <inheritdoc />
    public bool TryReplace(string name, AutomationPlan plan)
    {
        ArgumentHelpers.ThrowIfNull(plan);
        using var db = contexts.CreateDbContext();
        var row = db.Plans.FirstOrDefault(item => item.Name == name);
        if (row is null)
            return false;

        var stored = plan with { Name = name };
        row.Document = JsonSerializer.Serialize(stored, Json);
        row.StepCount = stored.Steps.Count;
        row.UpdatedUtc = DateTime.UtcNow;
        db.SaveChanges();
        InvalidateQueries();
        return true;
    }

    private void InvalidateQueries()
    {
        var cache = services.GetService<ICacheService>();
        if (cache is null)
            return;

        QueryCacheInvalidation.InvalidateQueryCachesForBroadEntityTypeAsync<AutomationPlanEntity>(cache).GetAwaiter().GetResult();
    }

    private static AutomationPlan? Read(string json)
    {
        var plan = JsonSerializer.Deserialize<AutomationPlan>(json, Json);
        return plan?.Steps is null ? null : plan;
    }
}
