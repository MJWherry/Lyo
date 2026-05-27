using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Format;
using Lyo.Authentication.OpenIdConnect.Discovery;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;

namespace Lyo.Authentication.OpenIdConnect.Tests;

/// <summary>
/// An in-process fake OpenID Connect IdP — handlers a discovery URL, JWKS URL, and a <c>/token</c> URL. Backed by an Ed25519 keypair to keep signing-validation tests free of RSA setup
/// noise. Used inside the coordinator end-to-end test.
/// </summary>
internal sealed class FakeOidcIdentityProvider
{
    public const string Issuer = "https://fake-idp.test";
    public const string DiscoveryUrl = "https://fake-idp.test/.well-known/openid-configuration";
    public const string TokenEndpoint = "https://fake-idp.test/token";
    public const string AuthorizeEndpoint = "https://fake-idp.test/authorize";
    public const string JwksUri = "https://fake-idp.test/jwks";
    public const string Kid = "fake-kid-1";

    private readonly Ed25519PrivateKeyParameters _private;
    private readonly Ed25519PublicKeyParameters _public;

    private readonly ConcurrentDictionary<string, IDictionary<string, object?>> _claimsByCode = new(StringComparer.Ordinal);

    public FakeOidcIdentityProvider()
    {
        var generator = new Ed25519KeyPairGenerator();
        generator.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
        var pair = generator.GenerateKeyPair();
        _private = (Ed25519PrivateKeyParameters)pair.Private;
        _public = (Ed25519PublicKeyParameters)pair.Public;
    }

    public string IssueCode(IDictionary<string, object?> claims)
    {
        var code = Guid.NewGuid().ToString("N");
        _claimsByCode[code] = claims;
        return code;
    }

    public HttpClient CreateHttpClient() => new(new Handler(this));

    /// <summary>Returns a fresh <see cref="HttpMessageHandler"/> wired to this IdP. Each call returns a new instance so the test harness can register one handler per typed HttpClient.</summary>
    public HttpMessageHandler CreateHandler() => new Handler(this);

    public string SignIdToken(IDictionary<string, object?> claims)
    {
        var header = new Dictionary<string, object?> {
            ["alg"] = "EdDSA",
            ["typ"] = "JWT",
            ["kid"] = Kid
        };

        var headerJson = JsonSerializer.SerializeToUtf8Bytes(header);
        var payloadJson = JsonSerializer.SerializeToUtf8Bytes(claims);
        var encodedHeader = Base64Url.Encode(headerJson);
        var encodedPayload = Base64Url.Encode(payloadJson);
        var signingInput = Encoding.ASCII.GetBytes($"{encodedHeader}.{encodedPayload}");
        var signer = new Ed25519Signer();
        signer.Init(true, _private);
        signer.BlockUpdate(signingInput, 0, signingInput.Length);
        var sig = signer.GenerateSignature();
        return $"{encodedHeader}.{encodedPayload}.{Base64Url.Encode(sig)}";
    }

    internal sealed class Handler : HttpMessageHandler
    {
        private readonly FakeOidcIdentityProvider _idp;

        public Handler(FakeOidcIdentityProvider idp) => _idp = idp;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var url = request.RequestUri!.ToString();
            if (url == DiscoveryUrl) {
                var doc = new OidcDiscoveryDocument {
                    Issuer = Issuer,
                    AuthorizationEndpoint = AuthorizeEndpoint,
                    TokenEndpoint = TokenEndpoint,
                    JwksUri = JwksUri
                };

                return new(System.Net.HttpStatusCode.OK) { Content = JsonContent.Create(doc) };
            }

            if (url == JwksUri) {
                var jwks = new OidcJwksDocument {
                    Keys = [
                        new() {
                            Kty = "OKP",
                            Crv = "Ed25519",
                            Use = "sig",
                            Alg = "EdDSA",
                            Kid = Kid,
                            X = Base64Url.Encode(_idp._public.GetEncoded())
                        }
                    ]
                };

                return new(System.Net.HttpStatusCode.OK) { Content = JsonContent.Create(jwks) };
            }

            if (url == TokenEndpoint) {
                var form = await request.Content!.ReadAsStringAsync(ct).ConfigureAwait(false);
                var pairs = form.Split('&');
                string? code = null;
                foreach (var p in pairs) {
                    var eq = p.IndexOf('=');
                    if (eq > 0 && string.Equals(p.AsSpan(0, eq).ToString(), "code", StringComparison.Ordinal))
                        code = System.Net.WebUtility.UrlDecode(p[(eq + 1)..]);
                }

                if (code is null || !_idp._claimsByCode.TryRemove(code, out var claims))
                    return new(System.Net.HttpStatusCode.BadRequest);

                var idToken = _idp.SignIdToken(claims);
                var response = new {
                    access_token = "fake-access",
                    id_token = idToken,
                    token_type = "Bearer",
                    expires_in = 3600
                };

                return new(System.Net.HttpStatusCode.OK) { Content = JsonContent.Create(response) };
            }

            return new(System.Net.HttpStatusCode.NotFound);
        }
    }
}
