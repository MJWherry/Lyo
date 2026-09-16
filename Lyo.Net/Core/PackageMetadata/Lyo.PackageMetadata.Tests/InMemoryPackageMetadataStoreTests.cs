namespace Lyo.PackageMetadata.Tests;

public sealed class InMemoryPackageMetadataStoreTests
{
    private static readonly Guid PkgId = Guid.Parse("f47ac10b-58cc-4372-a567-0e02b2c3d479");

    [Fact]
    public async Task Register_Normalizes_LicenseExpressionSyntax_From_LicenseExpression()
    {
        var store = new InMemoryPackageMetadataStore();
        var meta = new PackageMetadata(PkgId, PackageEcosystem.NuGet, "MyPkg", "1.0", LicenseExpression: "MIT OR Apache-2.0");
        store.Register(["LicDemo."], meta);
        var result = await store.TryGetForFrameAsync("", "LicDemo.Types.T", TestCt);
        Assert.NotNull(result);
        Assert.Equal("or", result.LicenseExpressionSyntax!.Kind);
        Assert.Equal("MIT", result.LicenseExpressionSyntax.Left!.Identifier);
        Assert.Equal("Apache-2.0", result.LicenseExpressionSyntax.Right!.Identifier);
    }

    [Fact]
    public async Task TryGetForFrameAsync_ReturnsMetadata_When_Prefix_Matches()
    {
        var store = new InMemoryPackageMetadataStore();
        var meta = new PackageMetadata(PkgId, PackageEcosystem.NuGet, "MyPkg", "1.0");
        store.Register(["MyCompany.MyApp."], meta);
        var result = await store.TryGetForFrameAsync("", "MyCompany.MyApp.Services.OrderService.Get", TestCt);
        Assert.NotNull(result);
        Assert.Equal(PkgId, result.Id);
        Assert.Equal("1.0", result.Version);
    }

    [Fact]
    public async Task TryGetForFrameAsync_Normalises_Prefix_Without_Trailing_Dot()
    {
        var store = new InMemoryPackageMetadataStore();
        store.Register(["Foo"], new(PkgId, PackageEcosystem.NuGet, "Foo"));
        var result = await store.TryGetForFrameAsync("", "Foo.Bar.Baz", TestCt);
        Assert.NotNull(result);
        Assert.Equal("Foo", result.Name);
    }

    [Fact]
    public async Task TryGetForFrameAsync_Longest_Prefix_Wins()
    {
        var store = new InMemoryPackageMetadataStore();
        var shortMeta = new PackageMetadata(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), PackageEcosystem.NuGet, "Short");
        var longMeta = new PackageMetadata(Guid.Parse("bbbbbbbb-bbbb-cccc-dddd-eeeeeeeeeeee"), PackageEcosystem.NuGet, "Long");
        store.Register(["A.B."], shortMeta);
        store.Register(["A.B.C."], longMeta);
        var result = await store.TryGetForFrameAsync("", "A.B.C.DMethod", TestCt);
        Assert.NotNull(result);
        Assert.Equal("Long", result.Name);
    }

    [Fact]
    public async Task TryGetForFrameAsync_Returns_Null_When_No_Match()
    {
        var store = new InMemoryPackageMetadataStore();
        store.Register(["Other."], new(PkgId, PackageEcosystem.NuGet, "X"));
        var result = await store.TryGetForFrameAsync("", "Unrelated.Type.Method", TestCt);
        Assert.Null(result);
    }

    [Fact]
    public async Task RegisterManyAsync_Combines_All_Registrations()
    {
        var store = new InMemoryPackageMetadataStore();
        await store.RegisterManyAsync(
        [
            new(["Alpha."], new(Guid.Parse("11111111-1111-1111-1111-111111111111"), PackageEcosystem.Maven, "A")),
            new(["Beta."], new(Guid.Parse("22222222-2222-2222-2222-222222222222"), PackageEcosystem.NuGet, "B"))
        ], TestCt);

        var a = await store.TryGetForFrameAsync("", "Alpha.X.Y", TestCt);
        var b = await store.TryGetForFrameAsync("", "Beta.Z", TestCt);
        Assert.Equal("A", a!.Name);
        Assert.Equal(PackageEcosystem.Maven, a.Ecosystem);
        Assert.Equal("B", b!.Name);
        Assert.Equal(PackageEcosystem.NuGet, b.Ecosystem);
    }

    [Fact]
    public async Task Register_PrefixOwnedByOtherPackage_LastRegisterWins()
    {
        var store = new InMemoryPackageMetadataStore();
        var first = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
        var second = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
        store.Register(["Shared.", "FirstOnly."], new(first, PackageEcosystem.NuGet, "First"));
        store.Register(["Shared."], new(second, PackageEcosystem.NuGet, "Second"));

        var shared = await store.TryGetForFrameAsync("", "Shared.Type.M", TestCt);
        var only = await store.TryGetForFrameAsync("", "FirstOnly.Type.M", TestCt);

        Assert.Equal("Second", shared!.Name);
        Assert.Equal("First", only!.Name);
    }

    [Fact]
    public async Task Register_Replaces_Prefixes_For_Same_Package_Id()
    {
        var store = new InMemoryPackageMetadataStore();
        var id = Guid.Parse("33333333-3333-3333-3333-333333333333");
        store.Register(["Old."], new(id, PackageEcosystem.NuGet, "Pkg", "1"));
        store.Register(["New."], new(id, PackageEcosystem.NuGet, "Pkg", "2"));
        var o = await store.TryGetForFrameAsync("", "Old.X", TestCt);
        var n = await store.TryGetForFrameAsync("", "New.X", TestCt);
        Assert.Null(o);
        Assert.NotNull(n);
        Assert.Equal("2", n.Version);
    }

    [Fact]
    public async Task TryGetManyForStrippedMethodPrefixesAsync_Empty_Returns_Empty_Map()
    {
        var store = new InMemoryPackageMetadataStore();
        store.Register(["X."], new(PkgId, PackageEcosystem.NuGet, "X"));
        var map = await store.TryGetManyForStrippedMethodPrefixesAsync([], TestCt);
        Assert.Empty(map);
    }

    [Fact]
    public async Task TryGetManyForStrippedMethodPrefixesAsync_Matches_TryGetForFrameAsync_Per_Key()
    {
        var store = new InMemoryPackageMetadataStore();
        store.Register(["Alpha."], new(Guid.Parse("11111111-1111-1111-1111-111111111111"), PackageEcosystem.NuGet, "A"));
        store.Register(["Beta."], new(Guid.Parse("22222222-2222-2222-2222-222222222222"), PackageEcosystem.NuGet, "B"));
        var keys = new[] { "Alpha.X.Y", "Beta.Z", "No.Match" };
        var bulk = await store.TryGetManyForStrippedMethodPrefixesAsync(keys, TestCt);
        foreach (var key in keys) {
            var single = await store.TryGetForFrameAsync("", key, TestCt);
            Assert.Equal(single, bulk[key]);
        }
    }

    [Fact]
    public async Task TryGetManyForStrippedMethodPrefixesAsync_Deduplicates_Keys_In_Result()
    {
        var store = new InMemoryPackageMetadataStore();
        store.Register(["Dup."], new(PkgId, PackageEcosystem.NuGet, "Pkg"));
        var map = await store.TryGetManyForStrippedMethodPrefixesAsync(["Dup.A.M", "Dup.A.M", "Dup.B.M"], TestCt);
        Assert.Equal(2, map.Count);
        Assert.Equal("Pkg", map["Dup.A.M"]!.Name);
        Assert.Equal("Pkg", map["Dup.B.M"]!.Name);
    }
}