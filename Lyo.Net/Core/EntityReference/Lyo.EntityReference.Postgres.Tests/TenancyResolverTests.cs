using Lyo.EntityReference.Models;

namespace Lyo.EntityReference.Postgres.Tests;

public class TenancyResolverTests
{
    private static readonly Guid CallerTenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FeatureDefault = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid GlobalDefault = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void Resolve_NullFeature_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => TenancyResolver.Resolve(CallerTenant, null!, new EntityRefOptions()));
        Assert.Equal("feature", ex.ParamName);
    }

    [Fact]
    public void Resolve_NullGlobal_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => TenancyResolver.Resolve(CallerTenant, new TenancyOptions(), null!));
        Assert.Equal("global", ex.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SystemOnly_AlwaysReturnsNull(bool hasCaller)
    {
        var feature = new TenancyOptions { Mode = TenancyMode.SystemOnly };
        var global = new EntityRefOptions { Mode = TenancyMode.SingleTenantDefault, DefaultTenantId = GlobalDefault };
        var result = TenancyResolver.Resolve(hasCaller ? CallerTenant : null, feature, global);
        Assert.Null(result);
    }

    [Fact]
    public void SystemOnly_FromGlobalInheritance_ReturnsNull()
    {
        var feature = new TenancyOptions();
        var global = new EntityRefOptions { Mode = TenancyMode.SystemOnly };
        Assert.Null(TenancyResolver.Resolve(CallerTenant, feature, global));
    }

    [Fact]
    public void MultiTenantStrict_NullCaller_Throws()
    {
        var feature = new TenancyOptions { Mode = TenancyMode.MultiTenantStrict };
        var global = new EntityRefOptions { DefaultTenantId = GlobalDefault };
        var ex = Assert.Throws<ArgumentNullException>(() => TenancyResolver.Resolve(null, feature, global));
        Assert.Equal("tenantId", ex.ParamName);
    }

    [Fact]
    public void MultiTenantStrict_EmptyCaller_Throws()
    {
        var feature = new TenancyOptions { Mode = TenancyMode.MultiTenantStrict };
        var global = new EntityRefOptions { DefaultTenantId = GlobalDefault };
        Assert.Throws<ArgumentNullException>(() => TenancyResolver.Resolve(Guid.Empty, feature, global));
    }

    [Fact]
    public void MultiTenantStrict_NonEmptyCaller_ReturnsCaller()
    {
        var feature = new TenancyOptions { Mode = TenancyMode.MultiTenantStrict, DefaultTenantId = FeatureDefault };
        var global = new EntityRefOptions { DefaultTenantId = GlobalDefault };
        Assert.Equal(CallerTenant, TenancyResolver.Resolve(CallerTenant, feature, global));
    }

    [Fact]
    public void MultiTenantStrict_FromGlobalInheritance_NullCallerThrows()
    {
        var feature = new TenancyOptions();
        var global = new EntityRefOptions { Mode = TenancyMode.MultiTenantStrict };
        Assert.Throws<ArgumentNullException>(() => TenancyResolver.Resolve(null, feature, global));
    }

    [Fact]
    public void SingleTenantDefault_NonEmptyCaller_ReturnsCaller()
    {
        var feature = new TenancyOptions { Mode = TenancyMode.SingleTenantDefault, DefaultTenantId = FeatureDefault };
        var global = new EntityRefOptions { DefaultTenantId = GlobalDefault };
        Assert.Equal(CallerTenant, TenancyResolver.Resolve(CallerTenant, feature, global));
    }

    [Fact]
    public void SingleTenantDefault_NullCaller_FeatureDefaultOverridesGlobal()
    {
        var feature = new TenancyOptions { Mode = TenancyMode.SingleTenantDefault, DefaultTenantId = FeatureDefault };
        var global = new EntityRefOptions { DefaultTenantId = GlobalDefault };
        Assert.Equal(FeatureDefault, TenancyResolver.Resolve(null, feature, global));
    }

    [Fact]
    public void SingleTenantDefault_EmptyCaller_FeatureDefaultOverridesGlobal()
    {
        var feature = new TenancyOptions { Mode = TenancyMode.SingleTenantDefault, DefaultTenantId = FeatureDefault };
        var global = new EntityRefOptions { DefaultTenantId = GlobalDefault };
        Assert.Equal(FeatureDefault, TenancyResolver.Resolve(Guid.Empty, feature, global));
    }

    [Fact]
    public void SingleTenantDefault_NullCaller_FallsBackToGlobalWhenFeatureUnset()
    {
        var feature = new TenancyOptions { Mode = TenancyMode.SingleTenantDefault };
        var global = new EntityRefOptions { DefaultTenantId = GlobalDefault };
        Assert.Equal(GlobalDefault, TenancyResolver.Resolve(null, feature, global));
    }

    [Fact]
    public void SingleTenantDefault_FromGlobalInheritance_NullCallerUsesGlobalDefault()
    {
        var feature = new TenancyOptions();
        var global = new EntityRefOptions { Mode = TenancyMode.SingleTenantDefault, DefaultTenantId = GlobalDefault };
        Assert.Equal(GlobalDefault, TenancyResolver.Resolve(null, feature, global));
    }

    [Fact]
    public void FeatureMode_OverridesGlobalMode()
    {
        var feature = new TenancyOptions { Mode = TenancyMode.MultiTenantStrict };
        var global = new EntityRefOptions { Mode = TenancyMode.SingleTenantDefault, DefaultTenantId = GlobalDefault };
        Assert.Throws<ArgumentNullException>(() => TenancyResolver.Resolve(null, feature, global));
    }
}
