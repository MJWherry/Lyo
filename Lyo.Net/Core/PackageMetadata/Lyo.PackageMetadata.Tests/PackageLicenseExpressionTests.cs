namespace Lyo.PackageMetadata.Tests;

public sealed class PackageLicenseExpressionTests
{
    [Fact]
    public void TryParseSyntax_Null_Or_Whitespace_Returns_Null()
    {
        Assert.Null(PackageLicenseExpression.TryParseSyntax(null));
        Assert.Null(PackageLicenseExpression.TryParseSyntax(""));
        Assert.Null(PackageLicenseExpression.TryParseSyntax("   "));
    }

    [Fact]
    public void TryParseSyntax_Or_Preserves_Structure()
    {
        var syn = PackageLicenseExpression.TryParseSyntax("MIT OR Apache-2.0");
        Assert.NotNull(syn);
        Assert.Equal("or", syn.Kind);
        Assert.Equal("license", syn.Left!.Kind);
        Assert.Equal("MIT", syn.Left.Identifier);
        Assert.Equal("license", syn.Right!.Kind);
        Assert.Equal("Apache-2.0", syn.Right.Identifier);
    }

    [Fact]
    public void TryParseSyntax_Nested_Parentheses_Preserves_And_Or()
    {
        var syn = PackageLicenseExpression.TryParseSyntax("(MIT OR Apache-2.0) AND BSD-3-Clause");
        Assert.NotNull(syn);
        Assert.Equal("and", syn.Kind);
        Assert.Equal("or", syn.Left!.Kind);
        Assert.Equal("BSD-3-Clause", syn.Right!.Identifier);
        Assert.Equal("license", syn.Left.Left!.Kind);
        Assert.Equal("MIT", syn.Left.Left.Identifier);
        Assert.Equal("license", syn.Left.Right!.Kind);
        Assert.Equal("Apache-2.0", syn.Left.Right.Identifier);
    }

    [Fact]
    public void TryParseSyntax_With_Places_Exception_Under_With()
    {
        var syn = PackageLicenseExpression.TryParseSyntax("GPL-2.0-only WITH GCC-exception-2.0");
        Assert.NotNull(syn);
        Assert.Equal("with", syn.Kind);
        Assert.Equal("license", syn.InnerLicense!.Kind);
        Assert.Equal("GPL-2.0-only", syn.InnerLicense.Identifier);
        Assert.Equal("exception", syn.InnerException!.Kind);
        Assert.Equal("GCC-exception-2.0", syn.InnerException.Identifier);
    }

    [Fact]
    public void TryGetSpdxLicenseIdentifiers_Complementary_Flat_List()
    {
        var ids = PackageLicenseExpression.TryGetSpdxLicenseIdentifiers("MIT OR Apache-2.0");
        Assert.NotNull(ids);
        Assert.Equal(["Apache-2.0", "MIT"], ids);
    }

    [Fact]
    public void TryGetSpdxLicenseIdentifiers_Invalid_Returns_Null()
    {
        Assert.Null(PackageLicenseExpression.TryGetSpdxLicenseIdentifiers("not-a-valid-expression!!!"));
        Assert.Null(PackageLicenseExpression.TryParseSyntax("not-a-valid-expression!!!"));
    }
}