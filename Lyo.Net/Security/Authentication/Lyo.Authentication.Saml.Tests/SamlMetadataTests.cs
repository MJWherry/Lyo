using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Lyo.Authentication.Sso.Completion;

namespace Lyo.Authentication.Saml.Tests;

public class SamlMetadataTests
{
    [Fact]
    public void Parse_RedirectBinding_IgnoresSpAndEncryptionCertificates()
    {
        var metadata = SamlMetadataParser.Parse(MetadataXml(SamlTestCertificate.Signing, SamlTestCertificate.Rollover));
        Assert.Equal(SamlTestProvider.Issuer, metadata.EntityId);
        Assert.Equal("https://idp.example/sso/redirect", metadata.RedirectSsoUrl);
        var cert = Assert.Single(metadata.SigningCertificates);
        Assert.Equal(SamlTestCertificate.Signing.RawData, cert);
    }

    [Fact]
    public async Task Metadata_RedirectToHttp_Rejects()
    {
        var cache = new SamlMetadataCache(new(new RedirectHandler()));
        var ex = await Assert.ThrowsAsync<ExternalLoginRejectedException>(() => cache.GetAsync(SamlTestProvider.Default, TestCt));
        Assert.Equal("SamlMetadata", ex.Reason);
    }

    [Fact]
    public async Task Metadata_OversizedXml_Rejects()
    {
        var cache = new SamlMetadataCache(new(new OversizedHandler()));
        var ex = await Assert.ThrowsAsync<ExternalLoginRejectedException>(() => cache.GetAsync(SamlTestProvider.Default, TestCt));
        Assert.Equal("SamlMetadata", ex.Reason);
    }

    [Fact]
    public async Task Metadata_FailedRefresh_KeepsLastGood()
    {
        var now = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);
        var clock = now;
        var handler = new SequenceHandler(MetadataXml(SamlTestCertificate.Signing, SamlTestCertificate.Rollover), true);
        var cache = new SamlMetadataCache(new(handler));
        cache.UseClock(() => clock);
        var first = await cache.GetAsync(SamlTestProvider.Default, TestCt);
        clock = now.AddHours(2);
        var second = await cache.GetAsync(SamlTestProvider.Default, TestCt);
        Assert.Equal(first.RedirectSsoUrl, second.RedirectSsoUrl);
        Assert.Equal(2, handler.Calls);
    }

    internal static string MetadataXml(X509Certificate2 signing, X509Certificate2 other)
    {
        var signingBody = Convert.ToBase64String(signing.RawData);
        var otherBody = Convert.ToBase64String(other.RawData);
        return $"""
                <md:EntityDescriptor xmlns:md="{SamlConstants.Metadata}" xmlns:ds="{SamlConstants.XmlDsig}" entityID="{SamlTestProvider.Issuer}">
                  <md:SPSSODescriptor>
                    <md:KeyDescriptor use="signing"><ds:KeyInfo><ds:X509Data><ds:X509Certificate>{otherBody}</ds:X509Certificate></ds:X509Data></ds:KeyInfo></md:KeyDescriptor>
                  </md:SPSSODescriptor>
                  <md:IDPSSODescriptor>
                    <md:KeyDescriptor use="encryption"><ds:KeyInfo><ds:X509Data><ds:X509Certificate>{otherBody}</ds:X509Certificate></ds:X509Data></ds:KeyInfo></md:KeyDescriptor>
                    <md:SingleSignOnService Binding="{SamlConstants.HttpPostBinding}" Location="https://idp.example/sso/post"/>
                    <md:KeyDescriptor use="signing"><ds:KeyInfo><ds:X509Data><ds:X509Certificate>{signingBody}</ds:X509Certificate></ds:X509Data></ds:KeyInfo></md:KeyDescriptor>
                    <md:SingleSignOnService Binding="{SamlConstants.HttpRedirectBinding}" Location="https://idp.example/sso/redirect"/>
                  </md:IDPSSODescriptor>
                </md:EntityDescriptor>
                """;
    }

    private sealed class RedirectHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.Redirect);
            response.Headers.Location = new("http://evil.example/metadata");
            return Task.FromResult(response);
        }
    }

    private sealed class OversizedHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = new string('A', SamlConstants.MaxDocumentBytes + 1);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/xml") });
        }
    }

    private sealed class SequenceHandler(string xml, bool failSecond) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            if (Calls > 1 && failSecond)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(xml, Encoding.UTF8, "application/xml") });
        }
    }
}