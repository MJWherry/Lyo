using System.IO.Compression;
using System.Text;
using System.Xml;
using Lyo.Authentication.Audit;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Sso.Completion;
using Lyo.Authentication.Sso.Protocol;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Authentication.Saml.Tests;

public class SamlLoginTicketTests
{
    [Fact]
    public async Task Start_RedirectContainsDeflatedAuthnRequestAndRelayState()
    {
        var protocol = SamlProtocolFactory.Create().Protocol;
        var start = await protocol.StartAsync("okta", "https://app.example/done", "browser", TestCt);
        var (xml, relay) = SamlRedirect.Read(start.RedirectUrl);
        Assert.Contains(SamlTestProvider.EntityId, xml);
        Assert.Contains(SamlTestProvider.Acs, xml);
        Assert.Contains(SamlTestProvider.SsoRedirect, xml);
        Assert.InRange(Encoding.UTF8.GetByteCount(relay), 1, 80);
        Assert.DoesNotContain("://", relay);
        Assert.Null(start.StateCookieValue);
        var raw = Convert.FromBase64String(QueryHelpers.ParseQuery(new Uri(start.RedirectUrl).Query)["SAMLRequest"].ToString());
        Assert.NotEqual(0x78, raw[0]);
    }

    [Fact]
    public async Task Start_AuthnRequest_ContainsIssuerAcsAndRedirectDestination()
    {
        var protocol = SamlProtocolFactory.Create().Protocol;
        var start = await protocol.StartAsync("okta", "/done", "browser", TestCt);
        var (xml, _) = SamlRedirect.Read(start.RedirectUrl);
        var request = SamlRedirect.Authn(xml);
        Assert.Equal(SamlTestProvider.EntityId, request.Issuer);
        Assert.Equal(SamlTestProvider.Acs, request.Acs);
        Assert.Equal(SamlTestProvider.SsoRedirect, request.Destination);
        Assert.Equal(SamlConstants.HttpPostBinding, request.ProtocolBinding);
        Assert.StartsWith(SamlTestProvider.SsoRedirect, start.RedirectUrl);
    }

    [Fact]
    public async Task Start_RelayState_IsOpaqueTicketUnder80Bytes()
    {
        var protocol = SamlProtocolFactory.Create().Protocol;
        var start = await protocol.StartAsync("okta", "/done", "api", TestCt);
        var relay = QueryHelpers.ParseQuery(new Uri(start.RedirectUrl).Query)["RelayState"].ToString();
        Assert.True(Encoding.UTF8.GetByteCount(relay) <= 80);
        Assert.DoesNotContain("/", relay);
        Assert.DoesNotContain(":", relay);
    }

    [Fact]
    public async Task Start_ExpiredTicket_Rejects()
    {
        var now = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);
        var clock = now;
        var protocol = SamlProtocolFactory.Create(() => clock).Protocol;
        var start = await protocol.StartAsync("okta", "/done", "browser", TestCt);
        var relay = QueryHelpers.ParseQuery(new Uri(start.RedirectUrl).Query)["RelayState"].ToString();
        clock = now.AddMinutes(16);
        var ex = await Assert.ThrowsAsync<ExternalLoginRejectedException>(() => protocol.CompleteAsync(Callback(relay, "YQ=="), TestCt));
        Assert.Equal("SamlTicket", ex.Reason);
    }

    private static SsoCallback Callback(string relay, string response)
        => new("okta", "POST", new Dictionary<string, string?>(), new Dictionary<string, string?> { ["RelayState"] = relay, ["SAMLResponse"] = response }, null);
}

public class SamlResponseValidatorTests
{
    [Fact]
    public void Complete_SignedAssertion_MapsNameIdAndEmail()
    {
        var assertion = Validate(new());
        Assert.Equal("okta", assertion.Provider);
        Assert.Equal("00uuser", assertion.Subject);
        Assert.Equal("a@example.com", assertion.Email);
        Assert.True(assertion.EmailVerified);
    }

    [Fact]
    public void Complete_BadSignature_Rejects()
    {
        var spec = new SamlResponseSpec { CorruptSignature = true };
        var ex = Assert.Throws<ExternalLoginRejectedException>(() => Validate(spec));
        Assert.Equal("SamlSignature", ex.Reason);
    }

    [Fact]
    public void Complete_SignedDecoyAndUnsignedAssertion_Rejects()
    {
        var spec = new SamlResponseSpec { SignDecoy = true, Signature = SamlSignatureKind.None };
        Assert.Throws<ExternalLoginRejectedException>(() => Validate(spec));
    }

    [Fact]
    public void Complete_DuplicateId_Rejects()
    {
        var spec = new SamlResponseSpec { DuplicateAssertionId = true };
        Assert.Throws<ExternalLoginRejectedException>(() => Validate(spec));
    }

    [Fact]
    public void Complete_SignatureReferenceMismatch_Rejects()
    {
        var spec = new SamlResponseSpec { ReferenceUri = "#_assert" };
        var xml = spec.ToXml().Replace("URI=\"#_assert\"", "URI=\"#_other\"", StringComparison.Ordinal);
        var ticket = new SamlLoginTicket("ticket", "okta", "_req", "/done", "browser", spec.Now.AddMinutes(10));
        Assert.Throws<ExternalLoginRejectedException>(() => new SamlResponseValidator().Validate(
            Convert.ToBase64String(Encoding.UTF8.GetBytes(xml)), ticket, SamlTestProvider.Default,
            new(SamlTestProvider.Issuer, SamlTestProvider.SsoRedirect, [SamlTestCertificate.Signing.RawData]), spec.Now, new InMemorySamlAssertionReplayCache()));
    }

    [Fact]
    public void Complete_CertNotInMetadata_Rejects()
    {
        var metadata = new SamlIdpMetadata(SamlTestProvider.Issuer, SamlTestProvider.SsoRedirect, [SamlTestCertificate.Rollover.RawData]);
        Assert.Throws<ExternalLoginRejectedException>(() => Validate(new(), metadata: metadata));
    }

    [Fact]
    public void Complete_RolloverCert_Accepts()
    {
        var metadata = new SamlIdpMetadata(SamlTestProvider.Issuer, SamlTestProvider.SsoRedirect, [SamlTestCertificate.Rollover.RawData, SamlTestCertificate.Signing.RawData]);
        var assertion = Validate(new(), metadata: metadata);
        Assert.Equal("00uuser", assertion.Subject);
    }

    [Fact]
    public void Complete_IssuerMismatch_Rejects() => Assert.Throws<ExternalLoginRejectedException>(() => Validate(new() { Issuer = "https://other.example" }));

    [Fact]
    public void Complete_AudienceMismatch_Rejects() => Assert.Throws<ExternalLoginRejectedException>(() => Validate(new() { Audience = "https://other.example" }));

    [Fact]
    public void Complete_MissingAudience_Rejects() => Assert.Throws<ExternalLoginRejectedException>(() => Validate(new() { IncludeAudience = false }));

    [Fact]
    public void Complete_ExpiredConditions_Rejects()
    {
        var spec = new SamlResponseSpec();
        spec.ConditionsNotOnOrAfter = spec.Now.AddMinutes(-5);
        Assert.Throws<ExternalLoginRejectedException>(() => Validate(spec));
    }

    [Fact]
    public void Complete_SubjectConfirmationExpired_Rejects()
    {
        var spec = new SamlResponseSpec();
        spec.ConfirmationNotOnOrAfter = spec.Now.AddMinutes(-5);
        Assert.Throws<ExternalLoginRejectedException>(() => Validate(spec));
    }

    [Fact]
    public void Complete_DestinationMismatch_Rejects() => Assert.Throws<ExternalLoginRejectedException>(() => Validate(new() { Destination = "https://evil.example/acs" }));

    [Fact]
    public void Complete_RecipientMismatch_Rejects() => Assert.Throws<ExternalLoginRejectedException>(() => Validate(new() { Recipient = "https://evil.example/acs" }));

    [Fact]
    public void Complete_InResponseToMismatch_Rejects() => Assert.Throws<ExternalLoginRejectedException>(() => Validate(new() { RequestId = "_other" }));

    [Fact]
    public void Complete_MissingInResponseTo_Rejects() => Assert.Throws<ExternalLoginRejectedException>(() => Validate(new() { IncludeInResponseTo = false }));

    [Fact]
    public async Task Complete_ReplayedRelayState_Rejects()
    {
        var completion = new CountingCompletion();
        var protocol = SamlProtocolFactory.Create(completion: completion).Protocol;
        var start = await protocol.StartAsync("okta", "/done", "browser", TestCt);
        var (xml, relay) = SamlRedirect.Read(start.RedirectUrl);
        var spec = new SamlResponseSpec { RequestId = SamlRedirect.RequestId(xml), Now = DateTime.UtcNow };
        var callback = new SsoCallback(
            "okta", "POST", new Dictionary<string, string?>(), new Dictionary<string, string?> { ["RelayState"] = relay, ["SAMLResponse"] = spec.ToBase64() }, null);

        await protocol.CompleteAsync(callback, TestCt);
        var ex = await Assert.ThrowsAsync<ExternalLoginRejectedException>(() => protocol.CompleteAsync(callback, TestCt));
        Assert.Equal("SamlTicket", ex.Reason);
        Assert.Equal(1, completion.Calls);
    }

    [Fact]
    public void Complete_StatusNotSuccess_Rejects()
        => Assert.Throws<ExternalLoginRejectedException>(() => Validate(new() { Status = "urn:oasis:names:tc:SAML:2.0:status:Responder" }));

    [Fact]
    public void Complete_HolderOfKey_Rejects()
        => Assert.Throws<ExternalLoginRejectedException>(() => Validate(new() { ConfirmationMethod = "urn:oasis:names:tc:SAML:2.0:cm:holder-of-key" }));

    [Fact]
    public void Complete_EncryptedAssertion_Rejects()
    {
        var ex = Assert.Throws<ExternalLoginRejectedException>(() => Validate(new() { IncludeEncryptedAssertion = true }));
        Assert.Equal("SamlEncrypted", ex.Reason);
    }

    [Fact]
    public void Complete_TransientNameId_Rejects() => Assert.Throws<ExternalLoginRejectedException>(() => Validate(new() { NameIdFormat = SamlConstants.TransientNameId }));

    [Fact]
    public void Complete_UnmappedEmailAttribute_DoesNotVerify()
    {
        var assertion = Validate(new() { EmailAttribute = "mail" });
        Assert.Null(assertion.Email);
        Assert.False(assertion.EmailVerified);
    }

    [Fact]
    public async Task Complete_MultiValueAllowedClaim_Accepts()
    {
        var spec = new SamlResponseSpec();
        spec.Attributes.Add(("groups", ["admins", "editors"]));
        var assertion = Validate(spec);
        var groups = Assert.IsAssignableFrom<IReadOnlyList<string>>(assertion.RawClaims["groups"]);
        Assert.Equal(["admins", "editors"], groups);
        var completion = new AllowEditorsCompletion();
        var issued = await completion.CompleteAsync(assertion, TestCt);
        Assert.Equal("editors", issued.AccessToken);
    }

    [Fact]
    public void Complete_Sha1Signature_Rejects() => Assert.Throws<ExternalLoginRejectedException>(() => Validate(new() { Signature = SamlSignatureKind.Sha1 }));

    [Fact]
    public void Complete_InclusiveCanonicalization_Rejects()
        => Assert.Throws<ExternalLoginRejectedException>(() => Validate(new() { Signature = SamlSignatureKind.InclusiveCanonicalization }));

    [Fact]
    public void Complete_DeflatedBody_Rejects()
    {
        var deflated = SamlAuthnRequest.Deflate(new SamlResponseSpec().ToXml());
        var ticket = new SamlLoginTicket("ticket", "okta", "_req", "/done", "browser", DateTime.UtcNow.AddMinutes(5));
        Assert.Throws<ExternalLoginRejectedException>(() => new SamlResponseValidator().Validate(
            deflated, ticket, SamlTestProvider.Default, SigningMetadata(), DateTime.UtcNow, new InMemorySamlAssertionReplayCache()));
    }

    private static ExternalLoginAssertion Validate(SamlResponseSpec spec, ISamlIdentityProvider? provider = null, SamlIdpMetadata? metadata = null)
    {
        var ticket = new SamlLoginTicket("ticket", provider?.Name ?? "okta", "_req", "/done", "browser", spec.Now.AddMinutes(10));
        return new SamlResponseValidator().Validate(
            spec.ToBase64(), ticket, provider ?? SamlTestProvider.Default, metadata ?? SigningMetadata(), spec.Now, new InMemorySamlAssertionReplayCache());
    }

    private static SamlIdpMetadata SigningMetadata() => new(SamlTestProvider.Issuer, SamlTestProvider.SsoRedirect, [SamlTestCertificate.Signing.RawData]);
}

internal static class SamlProtocolFactory
{
    public static (SamlSsoProtocol Protocol, InMemorySamlLoginTicketStore Tickets) Create(Func<DateTime>? clock = null, IExternalLoginCompletion? completion = null)
    {
        var tickets = clock is null ? new InMemorySamlLoginTicketStore() : new InMemorySamlLoginTicketStore(clock);
        var protocol = new SamlSsoProtocol(
            [SamlTestProvider.Default], new StaticSamlMetadata(new(SamlTestProvider.Issuer, SamlTestProvider.SsoRedirect, [SamlTestCertificate.Signing.RawData])), tickets,
            new InMemorySamlAssertionReplayCache(), new(), completion ?? new CountingCompletion(), new NullAuthAuditRecorder(), new NullAuthAuditContextAccessor(),
            NullLogger<SamlSsoProtocol>.Instance);

        return (protocol, tickets);
    }
}

internal static class SamlRedirect
{
    public static (string Xml, string RelayState) Read(string redirectUrl)
    {
        var query = QueryHelpers.ParseQuery(new Uri(redirectUrl).Query);
        var raw = Convert.FromBase64String(query["SAMLRequest"].ToString());
        using var input = new MemoryStream(raw);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(deflate);
        return (reader.ReadToEnd(), query["RelayState"].ToString());
    }

    public static string RequestId(string xml) => Authn(xml).Id;

    public static (string Id, string Issuer, string Acs, string Destination, string ProtocolBinding) Authn(string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);
        var root = document.DocumentElement!;
        var issuer = root.GetElementsByTagName("Issuer", SamlConstants.Assertion)[0]?.InnerText ?? "";
        return (root.GetAttribute("ID"), issuer, root.GetAttribute("AssertionConsumerServiceURL"), root.GetAttribute("Destination"), root.GetAttribute("ProtocolBinding"));
    }
}

internal sealed class CountingCompletion : IExternalLoginCompletion
{
    public int Calls { get; private set; }

    public Task<IssuedLyoJwt> CompleteAsync(ExternalLoginAssertion assertion, CancellationToken ct = default)
    {
        Calls++;
        return Task.FromResult(new IssuedLyoJwt("token", "jti", DateTime.UtcNow.AddMinutes(5), null, null));
    }
}

internal sealed class AllowEditorsCompletion : IExternalLoginCompletion
{
    public Task<IssuedLyoJwt> CompleteAsync(ExternalLoginAssertion assertion, CancellationToken ct = default)
    {
        if (assertion.RawClaims["groups"] is not IReadOnlyList<string> groups || !groups.Contains("editors"))
            throw new ExternalLoginRejectedException("UserNotProvisioned", "claim outside allowed set");

        return Task.FromResult(new IssuedLyoJwt("editors", "jti", DateTime.UtcNow.AddMinutes(5), null, null));
    }
}