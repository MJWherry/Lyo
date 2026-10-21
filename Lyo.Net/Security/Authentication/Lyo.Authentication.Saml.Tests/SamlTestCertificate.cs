using System.Security.Cryptography.X509Certificates;

namespace Lyo.Authentication.Saml.Tests;

public static class SamlTestCertificate
{
    internal const string CertificatePem = """
                                           -----BEGIN CERTIFICATE-----
                                           MIIDETCCAfmgAwIBAgIUIexo81uEj1Up6C6qSpBNVb4OUqYwDQYJKoZIhvcNAQEL
                                           BQAwGDEWMBQGA1UEAwwNbHlvLXNhbWwtdGVzdDAeFw0yNjEwMTgwMDU3MDhaFw0z
                                           NjEwMTUwMDU3MDhaMBgxFjAUBgNVBAMMDWx5by1zYW1sLXRlc3QwggEiMA0GCSqG
                                           SIb3DQEBAQUAA4IBDwAwggEKAoIBAQC1AENtLp1w50Oe9GGsO92tw8wmLGQrYMfp
                                           UWzVlSlBaCBneyt0+5gsy9UGnBW2N3UmhZ87Psg5gLrSSoui9YfYNShtypK0xT2G
                                           mwxZGfOvA3aqYOWv10/6betirJadizvywKW5q5YNgmYggFxgHyvzIS3bos2EAo4m
                                           KJuzdH9SNysu56YSjWER/jP+FIfVRYl6uF/g3fMBDNgX3qXjstUucKuyhsmySZkW
                                           7Z8FHJQRlWYF0Ta1frCOVgjela0p9B5fFJMtpLcaD2sIuZUJv8rGazepXr02G2J3
                                           aoSmkhisVfnIRee9OJ7KSxNt53Q6OpE7khZia8lhk6ms33NrH/0fAgMBAAGjUzBR
                                           MB0GA1UdDgQWBBTtauPmlZYFpTI7Nr+hE6asS2/yNzAfBgNVHSMEGDAWgBTtauPm
                                           lZYFpTI7Nr+hE6asS2/yNzAPBgNVHRMBAf8EBTADAQH/MA0GCSqGSIb3DQEBCwUA
                                           A4IBAQBjceoFuEJV4XyMPbieFHxGeg1rfnqobNArUikG4NTGKOF8YZ0nQ/PyapKo
                                           KrzCYhh7uU1y+ImwZlzMOqvrj6IUE+LYOVBO9EmubxWGs/nLQ5yzNHbcUzY7P6AM
                                           jlOuBlYXZy+BCyVhRzHgY0iuzkcrF/Q/u5718JBAHlHzZKg+mGq36FTc5lchVILq
                                           JB/Hw7RKyRGqEiajVEZZoXDG3kOY3LnC4O9O1zoE3//MwPxHufRunmMV4hGuH6Ya
                                           /PuBJKgJnuhLZIaclxZA+0QtIa1cVxmd/CRn47tQrmfzkRk6O8Sw/DLVSaAZjX84
                                           ApUXuP34E0CV6rnnBiCTJiVktuB1
                                           -----END CERTIFICATE-----
                                           """;

    internal const string PrivateKeyPem = """
                                          -----BEGIN PRIVATE KEY-----
                                          MIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKYwggSiAgEAAoIBAQC1AENtLp1w50Oe
                                          9GGsO92tw8wmLGQrYMfpUWzVlSlBaCBneyt0+5gsy9UGnBW2N3UmhZ87Psg5gLrS
                                          Soui9YfYNShtypK0xT2GmwxZGfOvA3aqYOWv10/6betirJadizvywKW5q5YNgmYg
                                          gFxgHyvzIS3bos2EAo4mKJuzdH9SNysu56YSjWER/jP+FIfVRYl6uF/g3fMBDNgX
                                          3qXjstUucKuyhsmySZkW7Z8FHJQRlWYF0Ta1frCOVgjela0p9B5fFJMtpLcaD2sI
                                          uZUJv8rGazepXr02G2J3aoSmkhisVfnIRee9OJ7KSxNt53Q6OpE7khZia8lhk6ms
                                          33NrH/0fAgMBAAECggEAAYwI4u1LyJ9l99iMTbyyZVliESoHrkoXC1Pl1c812XzV
                                          6zWllCdi67lhs3IME77EK7VnPEpA8CKYiHdPgDjrlZnrSGYW/FcfO6ST5bnrM2sG
                                          TZ9aG8mkVBHEufubXWB+Roi8/U6NtHwj1eQi+EWiymLDNkqmW+MzavjuvOSicDy5
                                          pOp7fr0LwbRMgFMYyWdUmLzODQ4Hpslkh55MHcNZrBlY97V5B1PswM1i1Oct4QUp
                                          xEDIq9JXuby645zKqL6CI7+GYsnNJ+u3RU+I0ryxSLvlRWGiqiDtj9tWCic0AQz5
                                          E6gTedLeRyrtafju7kvCE7knt09Xt3TuJDbzsY2qSQKBgQDmP7F+elA3zKbIaAd/
                                          c0tcqsw1vqrObLqYbqHmzlvGMyk4yElkKdzMAeMDR0oZ5IZQm7/CyOQyTjL90C4z
                                          ozHTiCsak18sIITtpOlZX9PohrAo6/MCRx81ZX8I7nXuSi/6H1Utoy+t6QOmy4NY
                                          ZANC/KI7xKKMLRxHw+cDxdrfOwKBgQDJPouxH7p8kFVu06wG6ds6S8n7+QfLWOM+
                                          Fs6HTG1RsqsM138RNGsyeItUo8RHSiik3GZMaJT0Y1DJN/GRgQCQ+UbnhFBoCpMm
                                          rF+tmbCXXOXzUoT56kZKWk6j2C/PpUbH8QPtROdFN7GUkm6qINp9S6CnJs1GXzJL
                                          QPAe+l/DbQKBgCcISSitx4eOFAAJUqwSeYYeFjxONFqGKCPLLXsZJIm3BpfvrDFk
                                          v19+OmPF5BP7n6FupdV5hOXaR96rOgna8olvng8FSK8MXDyFJmHWUvAqVRa62Rlb
                                          wTimGiEDe86HmRaH1ULFVUHgLJRMD9oeKkdi2IXVUdD9nLu1xCXplq8dAoGAPx8L
                                          Gcdl6uQqp9RkxNZdGO/krs/xjeqXfEYTCYUDVPjBhgmhpXfXl8EPksC7M9Ei5nLq
                                          81RqRC1SjYqqjoTz9IT7pGpv/fIzLZIrgHwQU6mjxqo/Y1QEMBYbTHDmri6dAOGr
                                          5aQTFjbd0EgNxfcAEQTDoyocgNIjKE96AX1uxBECgYBqQbQiFv9HsQPlqE87ZMrZ
                                          4QSnbIyH3zrLXkdt+COsoRQOm4QXtg+07paSaZdY3/6kZnd9Pxb/vxT9xRNnOh0o
                                          Odx7fDEk4Unw+hv6j0lmi9tsLCMIj7fUchLmqZqZ7Q8vHyOKHNkxtsdlYCpT25A9
                                          RxNcSxd+Qv2FgCMsiqnAgQ==
                                          -----END PRIVATE KEY-----
                                          """;

    internal const string RolloverCertificatePem = """
                                                   -----BEGIN CERTIFICATE-----
                                                   MIIDGTCCAgGgAwIBAgIUPQg1gEys/qX2pgTfzO5OuntNJowwDQYJKoZIhvcNAQEL
                                                   BQAwHDEaMBgGA1UEAwwRbHlvLXNhbWwtcm9sbG92ZXIwHhcNMjYxMDE4MDA1NzQ3
                                                   WhcNMzYxMDE1MDA1NzQ3WjAcMRowGAYDVQQDDBFseW8tc2FtbC1yb2xsb3ZlcjCC
                                                   ASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBALisoZxBlJ16TRMvttsXdE6Y
                                                   aVBNWobs1npUySseFGbCIrMUkF+kmsq+N1XrFVhTJkHaN328F0C7Xh8D2MUYDJcB
                                                   mNPakP8T8gnXWmOMEYxrQjEZunki9KTdqCIRC9jzcCUaAeeRrfOyz+K+7tfTnBOs
                                                   UZFD5vu/xJyFJTo7Wu3jhMyU3t05DZSGIeA1/enNm+UZSVL78rGVnQkOKHmgD4SL
                                                   ZHJCjdwhr1atFd3FBXSD9rsd6+XIKSR1JGLq+IOXbwHcPZsYny9i8NT7wKjt7EX1
                                                   dDIiQPZR3Vvs3W64CWv8Gn8/gWdlqv64a/5XZweVbHJy45VM+GtUOnvaKzNC0jMC
                                                   AwEAAaNTMFEwHQYDVR0OBBYEFEM/ZeygA+Q1Lt+SO3sEAj5iSepqMB8GA1UdIwQY
                                                   MBaAFEM/ZeygA+Q1Lt+SO3sEAj5iSepqMA8GA1UdEwEB/wQFMAMBAf8wDQYJKoZI
                                                   hvcNAQELBQADggEBABXzpuDlZxp/i+lJMjamInX37fvBDBBLN/dUpH7u7mOoooRm
                                                   d54g7J3p92urI5vey4uk2AGDC/Bmm18NOHoZKd2F4/cvlwrZjqGIGuWv8YxDaP6e
                                                   nISVXbgIWXX9kiEz1+ju5yL3RR7NA6djjGSu2Wz3yv743JXI45CwVw3DekYF9VUr
                                                   EC2TyVMBayDtZz0hN1GjRzcoaKCD/T2lsRyAjI6eLy4njB7fg1bXINVMAkLXPYkL
                                                   A+w47inbJbMDhJyJ4Rb5/Ig040u+c0SLJWBxpTn1dLTOxgGwzLmyMn1PTU9Pc3Fz
                                                   +MGB4IIZGErBryB4XWdlz7Tpitl7PwA8VSLoXbE=
                                                   -----END CERTIFICATE-----
                                                   """;

    public static X509Certificate2 Signing { get; } = X509Certificate2.CreateFromPem(CertificatePem, PrivateKeyPem);

    public static X509Certificate2 Rollover { get; } = X509Certificate2.CreateFromPem(RolloverCertificatePem);
}