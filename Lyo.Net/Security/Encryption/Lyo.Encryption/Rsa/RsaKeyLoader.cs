using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
#if !NET10_0_OR_GREATER
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
#endif

namespace Lyo.Encryption.Rsa;

public static class RsaKeyLoader
{
    public static RSA LoadFromPfx(string pfxPath, string password)
    {
#if NET10_0_OR_GREATER
        var cert = X509CertificateLoader.LoadPkcs12FromFile(pfxPath, password, X509KeyStorageFlags.Exportable);
        return cert.GetRSAPrivateKey() ?? throw new InvalidOperationException("PFX file does not contain a private RSA key.");
#else
        var cert = new X509Certificate2(pfxPath, password, X509KeyStorageFlags.Exportable);
        return cert.GetRSAPrivateKey() ?? throw new InvalidOperationException("PFX file does not contain a private RSA key.");
#endif
    }

    public static RSA LoadFromPemFiles(string publicKeyPath, string? privateKeyPath = null)
    {
#if NET10_0_OR_GREATER
        var rsa = RSA.Create();
        var publicPem = File.ReadAllText(publicKeyPath);
        rsa.ImportSubjectPublicKeyInfo(ReadPem(publicPem, "PUBLIC KEY"), out var _);
        if (string.IsNullOrEmpty(privateKeyPath))
            return rsa;

        var privatePem = File.ReadAllText(privateKeyPath);
        rsa.ImportPkcs8PrivateKey(ReadPem(privatePem, "PRIVATE KEY"), out var _);
        return rsa;
#else
        var rsa = RSA.Create();
        var publicPem = File.ReadAllText(publicKeyPath);
        ImportPublicPem(rsa, publicPem);
        if (string.IsNullOrEmpty(privateKeyPath))
            return rsa;

        var privatePem = File.ReadAllText(privateKeyPath);
        ImportPrivatePem(rsa, privatePem);
        return rsa;
#endif
    }

#if !NET10_0_OR_GREATER
    private static void ImportPublicPem(RSA rsa, string pem)
    {
        var reader = new PemReader(new StringReader(pem));
        var obj = reader.ReadObject() ?? throw new InvalidOperationException("Invalid PEM public key.");
        var rsaParams = obj switch {
            RsaKeyParameters rp => ToPublicParameters(rp),
            _ => throw new NotSupportedException("PEM does not contain an RSA public key.")
        };
        rsa.ImportParameters(rsaParams);
    }

    private static void ImportPrivatePem(RSA rsa, string pem)
    {
        var reader = new PemReader(new StringReader(pem));
        var obj = reader.ReadObject() ?? throw new InvalidOperationException("Invalid PEM private key.");
        var rsaParams = obj switch {
            RsaPrivateCrtKeyParameters crt => ToPrivateParameters(crt),
            AsymmetricCipherKeyPair pair when pair.Private is RsaPrivateCrtKeyParameters crt => ToPrivateParameters(crt),
            _ => throw new NotSupportedException("PEM does not contain an RSA private key.")
        };
        rsa.ImportParameters(rsaParams);
    }

    private static RSAParameters ToPublicParameters(RsaKeyParameters pub)
    {
        return new RSAParameters {
            Modulus = pub.Modulus.ToByteArrayUnsigned(),
            Exponent = pub.Exponent.ToByteArrayUnsigned()
        };
    }

    private static RSAParameters ToPrivateParameters(RsaPrivateCrtKeyParameters crt)
    {
        return new RSAParameters {
            Modulus = crt.Modulus.ToByteArrayUnsigned(),
            Exponent = crt.PublicExponent.ToByteArrayUnsigned(),
            D = crt.Exponent.ToByteArrayUnsigned(),
            P = crt.P.ToByteArrayUnsigned(),
            Q = crt.Q.ToByteArrayUnsigned(),
            DP = crt.DP.ToByteArrayUnsigned(),
            DQ = crt.DQ.ToByteArrayUnsigned(),
            InverseQ = crt.QInv.ToByteArrayUnsigned()
        };
    }
#endif

    private static byte[] ReadPem(string pem, string section)
    {
        var match = Regex.Match(pem, $"-----BEGIN {section}-----(.*?)-----END {section}-----", RegexOptions.Singleline);
        return !match.Success
            ? throw new FormatException($"PEM section '{section}' not found.")
            : Convert.FromBase64String(match.Groups[1].Value.Replace("\n", "").Replace("\r", "").Trim());
    }
}
