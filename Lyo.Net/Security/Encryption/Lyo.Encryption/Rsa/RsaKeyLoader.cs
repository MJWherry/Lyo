using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace Lyo.Encryption.Rsa;

public static class RsaKeyLoader
{
    public static RSA LoadFromPfx(string pfxPath, string password)
    {
        var cert = X509CertificateLoader.LoadPkcs12FromFile(pfxPath, password, X509KeyStorageFlags.Exportable);
        return cert.GetRSAPrivateKey() ?? throw new InvalidOperationException("PFX file does not contain a private RSA key.");
    }

    public static RSA LoadFromPemFiles(string publicKeyPath, string? privateKeyPath = null)
    {
        var rsa = RSA.Create();
        var publicPem = File.ReadAllText(publicKeyPath);
        rsa.ImportSubjectPublicKeyInfo(ReadPem(publicPem, "PUBLIC KEY"), out var _);
        if (string.IsNullOrEmpty(privateKeyPath))
            return rsa;

        var privatePem = File.ReadAllText(privateKeyPath);
        rsa.ImportPkcs8PrivateKey(ReadPem(privatePem, "PRIVATE KEY"), out var _);
        return rsa;
    }

    private static byte[] ReadPem(string pem, string section)
    {
        var match = Regex.Match(pem, $"-----BEGIN {section}-----(.*?)-----END {section}-----", RegexOptions.Singleline);
        return !match.Success
            ? throw new FormatException($"PEM section '{section}' not found.")
            : Convert.FromBase64String(match.Groups[1].Value.Replace("\n", "").Replace("\r", "").Trim());
    }
}