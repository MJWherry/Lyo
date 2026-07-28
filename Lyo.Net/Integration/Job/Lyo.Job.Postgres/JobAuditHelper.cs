using Lyo.Audit;
using Lyo.EntityReference.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Job.Postgres;

/// <summary>Helpers for recording job CRUD audit entries.</summary>
internal static class JobAuditHelper
{
    public static EntityRef? GetActor(IServiceProvider services)
    {
        var httpContextAccessor = services.GetService<IHttpContextAccessor>();
        var identity = httpContextAccessor?.HttpContext?.User?.Identity;
        if (identity is null || !identity.IsAuthenticated || string.IsNullOrWhiteSpace(identity.Name))
            return null;

        return new EntityRef("User", identity.Name);
    }

    public static void RecordCreated(IServiceProvider services, string entityType, Guid entityId, string? message = null)
        => RecordEvent(services, entityType, entityId, $"{entityType}.Created", message ?? $"{entityType} created");

    public static void RecordUpdated(IServiceProvider services, string entityType, Guid entityId, string? message = null)
        => RecordEvent(services, entityType, entityId, $"{entityType}.Updated", message ?? $"{entityType} updated");

    private static void RecordEvent(IServiceProvider services, string entityType, Guid entityId, string eventType, string message)
    {
        var recorder = services.GetService<IAuditRecorder>();
        if (recorder is null)
            return;

        var actor = GetActor(services);
        var evt = new AuditEvent(new(entityType, entityId.ToString()), eventType, message, actor);
        recorder.RecordEvent(evt);
    }
}