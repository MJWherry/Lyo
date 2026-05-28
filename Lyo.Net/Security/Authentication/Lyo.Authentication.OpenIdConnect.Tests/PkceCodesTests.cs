using System;
using System.Security.Cryptography;
using System.Text;
using Lyo.Authentication.Format;
using Lyo.Authentication.Models.Format;
using Lyo.Authentication.OpenIdConnect.Pkce;

namespace Lyo.Authentication.OpenIdConnect.Tests;

public sealed class PkceCodesTests
{
    [Fact]
    public void Generate_ProducesVerifierAndS256Challenge()
    {
        var codes = PkceCodes.Generate();
        Assert.False(string.IsNullOrEmpty(codes.Verifier));
        Assert.False(string.IsNullOrEmpty(codes.Challenge));
        Assert.True(Base64Url.IsValid(codes.Verifier));
        Assert.True(Base64Url.IsValid(codes.Challenge));
    }

    [Fact]
    public void Challenge_IsBase64UrlOfSha256OfVerifier()
    {
        var codes = PkceCodes.Generate();
        var expected = Base64Url.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(codes.Verifier)));
        Assert.Equal(expected, codes.Challenge);
    }

    [Fact]
    public void Generate_ProducesDistinctValues()
    {
        var a = PkceCodes.Generate();
        var b = PkceCodes.Generate();
        Assert.NotEqual(a.Verifier, b.Verifier);
        Assert.NotEqual(a.Challenge, b.Challenge);
    }
}
