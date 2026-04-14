namespace Lyo.Encryption.Tests;

public class TwoKeyDekValidationTests
{
    [Theory]
    [InlineData(EncryptionAlgorithm.AesGcm, "16, 24, or 32")]
    [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, "32")]
    [InlineData(EncryptionAlgorithm.AesCcm, "16, 24, or 32")]
    [InlineData(EncryptionAlgorithm.AesSiv, "32, 48, or 64")]
    [InlineData(EncryptionAlgorithm.XChaCha20Poly1305, "32")]
    public void DescribeValidSymmetricKeyMaterialSizes_SymmetricAlgorithms_ContainsExpectedSizes(EncryptionAlgorithm alg, string expectedFragment)
    {
        var s = TwoKeyDekValidation.DescribeValidSymmetricKeyMaterialSizes((byte)alg);
        Assert.Contains(expectedFragment, s, StringComparison.Ordinal);
    }

    [Fact]
    public void DescribeValidSymmetricKeyMaterialSizes_Rsa_MentionsNotInHeader()
    {
        var s = TwoKeyDekValidation.DescribeValidSymmetricKeyMaterialSizes((byte)EncryptionAlgorithm.Rsa);
        Assert.Contains("N/A", s, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("header", s, StringComparison.OrdinalIgnoreCase);
    }
}
