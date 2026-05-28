using Lyo.Authentication.OpenIdConnect.Pkce;
using Microsoft.AspNetCore.DataProtection;

namespace Lyo.Authentication.OpenIdConnect.Tests;

public sealed class StateNonceProtectorTests
{
    [Fact]
    public void Seal_And_Unseal_RoundTrip()
    {
        var protector = NewProtector();
        var input = new PkceState("verifier-abc", "nonce-xyz", "google", "/dashboard", "state-123");
        var sealedValue = protector.Seal(input);
        var roundTripped = protector.Unseal(sealedValue);
        Assert.NotNull(roundTripped);
        Assert.Equal(input, roundTripped);
    }

    [Fact]
    public void Unseal_RejectsTamperedInput()
    {
        var protector = NewProtector();
        var sealedValue = protector.Seal(new("v", "n", "google", "/", "s"));
        var tampered = sealedValue[..^2] + "AA";
        var result = protector.Unseal(tampered);
        Assert.Null(result);
    }

    [Fact]
    public void Unseal_RejectsValueFromDifferentProtector()
    {
        var p1 = NewProtector("App-A-" + Guid.NewGuid());
        var p2 = NewProtector("App-B-" + Guid.NewGuid());
        var sealedValue = p1.Seal(new("v", "n", "google", "/", "s"));
        var result = p2.Unseal(sealedValue);
        Assert.Null(result);
    }

    [Fact]
    public void Unseal_ReturnsNullOnEmpty()
    {
        var protector = NewProtector();
        Assert.Null(protector.Unseal(null));
        Assert.Null(protector.Unseal(string.Empty));
    }

    [Fact]
    public void GenerateState_ProducesUniqueHighEntropyValues()
    {
        var a = StateNonceProtector.GenerateState();
        var b = StateNonceProtector.GenerateState();
        Assert.NotEqual(a, b);
        Assert.True(a.Length >= 32);
    }

    private static StateNonceProtector NewProtector(string applicationName = "LyoTests") => new(DataProtectionProvider.Create(applicationName));
}