namespace Lyo.RequestSigning;

/// <summary>The request fields covered by a signature.</summary>
/// <param name="Method">HTTP method. Signed uppercased.</param>
/// <param name="Scheme">URL scheme. Signed lowercased.</param>
/// <param name="Host">Host as the request reports it. An explicit port stays. A default port is not added.</param>
/// <param name="PathAndQuery">Path and query string.</param>
/// <param name="Body">Raw body. An empty body is hashed as empty bytes.</param>
public sealed record RequestSignatureInput(string Method, string Scheme, string Host, string PathAndQuery, ReadOnlyMemory<byte> Body);