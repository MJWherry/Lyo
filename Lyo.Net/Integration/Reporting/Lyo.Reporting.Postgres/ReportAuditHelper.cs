using Lyo.Audit;
using Lyo.EntityReference.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Reporting.Postgres;

/// <summary>Helpers for recording reporting CRUD / generate audit entries.</summary>
public static class ReportAuditHelper
{
    public static EntityRef? GetActor(IServiceProvider services)
    {
        var httpContextAccessor = services.GetService<IHttpContextAccessor>();
        var identity = httpContextAccessor?.HttpContext?.User?.Identity;
        if (identity is null || !identity.IsAuthenticated || string.IsNullOrWhiteSpace(identity.Name))
            return null;

        return new EntityRef("User", identity.Name);
    }

    public static string GetActorName(IServiceProvider services, string? fallback = null)
    {
        var httpContextAccessor = services.GetService<IHttpContextAccessor>();
        var name = httpContextAccessor?.HttpContext?.User?.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(name))
            return name.Length > 50 ? name[..50] : name;

        return string.IsNullOrWhiteSpace(fallback) ? "Unknown" : (fallback.Length > 50 ? fallback[..50] : fallback);
    }

    public static void RecordCreated(IServiceProvider services, string entityType, Guid entityId, string? message = null)
        => RecordEvent(services, entityType, entityId, $"{entityType}.Created", message ?? $"{entityType} created");

    public static void RecordUpdated(IServiceProvider services, string entityType, Guid entityId, string? message = null)
        => RecordEvent(services, entityType, entityId, $"{entityType}.Updated", message ?? $"{entityType} updated");

    public static void RecordGenerated(IServiceProvider services, Guid generationId, string? message = null)
        => RecordEvent(services, "ReportGeneration", generationId, "ReportGeneration.Generated", message ?? "Report generated");

    private static void RecordEvent(IServiceProvider services, string entityType, Guid entityId, string eventType, string message)
    {
        var recorder = services.GetService<IAuditRecorder>();
        if (recorder is null)
            return;

        var actor = GetActor(services);
        var evt = new AuditEvent(new EntityRef(entityType, entityId.ToString()), eventType, message, actor);
        recorder.RecordEvent(evt);
    }
}
