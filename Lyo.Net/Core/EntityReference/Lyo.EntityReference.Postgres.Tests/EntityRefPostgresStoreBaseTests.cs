using Lyo.EntityReference.Models;
using Microsoft.Extensions.Options;

namespace Lyo.EntityReference.Postgres.Tests;

public class EntityRefPostgresStoreBaseTests
{
    private static readonly Guid Tenant = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid GlobalDefault = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void Ctor_NullEntityRefOptions_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new RequiredTenantStore(null!, new TenancyOptions()));

    [Fact]
    public void Ctor_NullTenancyOptions_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new RequiredTenantStore(Options.Create(new EntityRefOptions()), null!));

    [Fact]
    public void Ctor_RequiresNonNullTenant_RejectsSystemOnlyFromFeature()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new RequiredTenantStore(
                Options.Create(new EntityRefOptions()),
                new TenancyOptions { Mode = TenancyMode.SystemOnly }));
        Assert.Contains(nameof(RequiredTenantStore), ex.Message, StringComparison.Ordinal);
        Assert.Contains("SystemOnly", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Ctor_RequiresNonNullTenant_RejectsSystemOnlyFromGlobal()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new RequiredTenantStore(
                Options.Create(new EntityRefOptions { Mode = TenancyMode.SystemOnly }),
                new TenancyOptions()));
        Assert.Contains(nameof(RequiredTenantStore), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Ctor_RequiresNonNullTenant_AcceptsSingleTenantDefault()
    {
        var store = new RequiredTenantStore(
            Options.Create(new EntityRefOptions { Mode = TenancyMode.SingleTenantDefault, DefaultTenantId = GlobalDefault }),
            new TenancyOptions());
        Assert.Equal(GlobalDefault, store.PublicResolveTenant(null));
    }

    [Fact]
    public void Ctor_RequiresNonNullTenant_AcceptsMultiTenantStrict()
    {
        var store = new RequiredTenantStore(
            Options.Create(new EntityRefOptions()),
            new TenancyOptions { Mode = TenancyMode.MultiTenantStrict });
        Assert.Equal(Tenant, store.PublicResolveTenant(Tenant));
    }

    [Fact]
    public void Ctor_NullableTenantStore_AllowsSystemOnly()
    {
        var store = new NullableTenantStore(
            Options.Create(new EntityRefOptions()),
            new TenancyOptions { Mode = TenancyMode.SystemOnly });
        Assert.Null(store.PublicResolveTenantOrNull(Tenant));
    }

    [Fact]
    public void ResolveTenant_StrictMode_ThrowsForNullCaller()
    {
        var store = new RequiredTenantStore(
            Options.Create(new EntityRefOptions()),
            new TenancyOptions { Mode = TenancyMode.MultiTenantStrict });
        Assert.Throws<ArgumentNullException>(() => store.PublicResolveTenant(null));
    }

    [Fact]
    public void ResolveTenantOrNull_StrictMode_ThrowsForNullCaller()
    {
        var store = new NullableTenantStore(
            Options.Create(new EntityRefOptions()),
            new TenancyOptions { Mode = TenancyMode.MultiTenantStrict });
        Assert.Throws<ArgumentNullException>(() => store.PublicResolveTenantOrNull(null));
    }

    [Fact]
    public void ResolveTenantOrNull_SingleTenantDefault_NullCallerUsesDefault()
    {
        var store = new NullableTenantStore(
            Options.Create(new EntityRefOptions { DefaultTenantId = GlobalDefault }),
            new TenancyOptions());
        Assert.Equal(GlobalDefault, store.PublicResolveTenantOrNull(null));
    }

    private sealed class RequiredTenantStore(IOptions<EntityRefOptions> entityRefOptions, TenancyOptions tenancy)
        : EntityRefPostgresStoreBase(entityRefOptions, tenancy)
    {
        public Guid PublicResolveTenant(Guid? tenantId) => ResolveTenant(tenantId);
    }

    private sealed class NullableTenantStore(IOptions<EntityRefOptions> entityRefOptions, TenancyOptions tenancy)
        : EntityRefPostgresStoreBase(entityRefOptions, tenancy, interceptors: null, requiresNonNullTenant: false)
    {
        public Guid? PublicResolveTenantOrNull(Guid? tenantId) => ResolveTenantOrNull(tenantId);
    }
}
