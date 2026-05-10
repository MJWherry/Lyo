using System.Diagnostics;

namespace Lyo.EntityReference.Models;

/// <summary>Lifecycle point for <see cref="IEntityRefActionInterceptor"/>.</summary>
public enum EntityRefActionKind
{
    /// <summary>Before an entity is inserted or updated.</summary>
    BeforePersist,

    /// <summary>After <c>SaveChanges</c> completes for insert or update.</summary>
    AfterPersist,

    /// <summary>Before soft-delete fields are applied.</summary>
    BeforeSoftDelete,

    /// <summary>After <c>SaveChanges</c> completes for soft-delete.</summary>
    AfterSoftDelete
}

/// <summary>Payload passed to entity-ref interceptors.</summary>
[DebuggerDisplay("{Kind}, Tenant={TenantId}, Module={ModuleKey,nq}")]
public sealed class EntityRefActionContext
{
    /// <summary>Interceptor phase.</summary>
    public required EntityRefActionKind Kind { get; init; }

    /// <summary>Resolved tenant id written to the row.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Logical module key (for example Favorite, Tag, Comment).</summary>
    public required string ModuleKey { get; init; }

    /// <summary>EF entity or domain record being persisted, when applicable.</summary>
    public object? Entity { get; init; }

    /// <inheritdoc />
    public override string ToString() => $"{Kind}: Tenant={TenantId}, Module={ModuleKey}";
}
