using Lyo.Exceptions.Models;
using Lyo.Keystore;

namespace Lyo.Encryption.Tests;

public class SecureKeyGeneratorTests
{
    [Fact]
    public void GenerateKey_DefaultSize_Produces32Bytes()
    {
        var key = SecureKeyGenerator.GenerateKey();
        Assert.Equal(32, key.Length);
    }

    [Fact]
    public void GenerateKey_CustomSize_ProducesCorrectSize()
    {
        var key16 = SecureKeyGenerator.GenerateKey(16);
        var key64 = SecureKeyGenerator.GenerateKey(64);
        Assert.Equal(16, key16.Length);
        Assert.Equal(64, key64.Length);
    }

    [Fact]
    public void GenerateKey_DifferentCalls_ProduceDifferentKeys()
    {
        var key1 = SecureKeyGenerator.GenerateKey();
        var key2 = SecureKeyGenerator.GenerateKey();
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateKey_TooSmall_Throws()
    {
        var ex = Assert.Throws<ArgumentOutsideRangeException>(() => SecureKeyGenerator.GenerateKey(15));
        Assert.Equal(15, ex.ActualValue);
        Assert.Equal(16, ex.MinValue);
        Assert.Equal(64, ex.MaxValue);
    }

    [Fact]
    public void GenerateKey_TooLarge_Throws()
    {
        var ex = Assert.Throws<ArgumentOutsideRangeException>(() => SecureKeyGenerator.GenerateKey(65));
        Assert.Equal(65, ex.ActualValue);
        Assert.Equal(16, ex.MinValue);
        Assert.Equal(64, ex.MaxValue);
    }

    [Fact]
    public void GenerateKey_MinimumSize_Works()
    {
        var key = SecureKeyGenerator.GenerateKey(16);
        Assert.Equal(16, key.Length);
    }

    [Fact]
    public void GenerateKey_MaximumSize_Works()
    {
        var key = SecureKeyGenerator.GenerateKey(64);
        Assert.Equal(64, key.Length);
    }

    [Fact]
    public void GenerateKeyString_DefaultLength_Produces32Chars()
    {
        var key = SecureKeyGenerator.GenerateKeyString();
        Assert.Equal(32, key.Length);
    }

    [Fact]
    public void GenerateKeyString_CustomLength_ProducesCorrectLength()
    {
        var key = SecureKeyGenerator.GenerateKeyString(64);
        Assert.Equal(64, key.Length);
    }

    [Fact]
    public void GenerateKeyString_DifferentCalls_ProduceDifferentKeys()
    {
        var key1 = SecureKeyGenerator.GenerateKeyString();
        var key2 = SecureKeyGenerator.GenerateKeyString();
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateKeyString_WithSpecialChars_ContainsSpecialChars()
    {
        var key = SecureKeyGenerator.GenerateKeyString(100);
        var hasSpecial = key.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c));
        Assert.True(hasSpecial);
    }

    [Fact]
    public void GenerateKeyString_WithoutSpecialChars_NoSpecialChars()
    {
        var key = SecureKeyGenerator.GenerateKeyString(100, false);
        var hasSpecial = key.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c));
        Assert.False(hasSpecial);
    }

    [Fact]
    public void GenerateKeyString_TooShort_Throws()
    {
        var ex = Assert.Throws<ArgumentOutsideRangeException>(() => SecureKeyGenerator.GenerateKeyString(15));
        Assert.Equal(15, ex.ActualValue);
        Assert.Equal(16, ex.MinValue);
    }

    [Fact]
    public void GenerateKeyWithSalt_DefaultSizes_ProducesCorrectSizes()
    {
        var (key, salt) = SecureKeyGenerator.GenerateKeyWithSalt();
        Assert.Equal(32, key.Length);
        Assert.Equal(16, salt.Length);
    }

    [Fact]
    public void GenerateKeyWithSalt_CustomSizes_ProducesCorrectSizes()
    {
        var (key, salt) = SecureKeyGenerator.GenerateKeyWithSalt(24, 12);
        Assert.Equal(24, key.Length);
        Assert.Equal(12, salt.Length);
    }

    [Fact]
    public void GenerateKeyWithSalt_DifferentCalls_ProduceDifferentValues()
    {
        var (key1, salt1) = SecureKeyGenerator.GenerateKeyWithSalt();
        var (key2, salt2) = SecureKeyGenerator.GenerateKeyWithSalt();
        Assert.NotEqual(key1, key2);
        Assert.NotEqual(salt1, salt2);
    }

    [Fact]
    public void GenerateNonce_DefaultSize_Produces12Bytes()
    {
        var nonce = SecureKeyGenerator.GenerateNonce();
        Assert.Equal(12, nonce.Length);
    }

    [Fact]
    public void GenerateNonce_CustomSize_ProducesCorrectSize()
    {
        var nonce = SecureKeyGenerator.GenerateNonce(16);
        Assert.Equal(16, nonce.Length);
    }

    [Fact]
    public void GenerateNonce_DifferentCalls_ProduceDifferentNonces()
    {
        var nonce1 = SecureKeyGenerator.GenerateNonce();
        var nonce2 = SecureKeyGenerator.GenerateNonce();
        Assert.NotEqual(nonce1, nonce2);
    }

    [Fact]
    public void GenerateNonce_TooSmall_Throws()
    {
        var ex = Assert.Throws<ArgumentOutsideRangeException>(() => SecureKeyGenerator.GenerateNonce(7));
        Assert.Equal(7, ex.ActualValue);
        Assert.Equal(8, ex.MinValue);
    }

    [Fact]
    public void GenerateKey_ProducesRandomKeys()
    {
        // Generate many keys and verify they're all different
        var keys = new byte[100][];
        for (var i = 0; i < 100; i++)
            keys[i] = SecureKeyGenerator.GenerateKey();

        var uniqueKeys = keys.Distinct(new ByteArrayComparer()).Count();
        Assert.True(uniqueKeys > 95); // Should have high uniqueness
    }

    private class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[]? x, byte[]? y)
        {
            if (x == null || y == null)
                return x == y;

            return x.SequenceEqual(y);
        }

        public int GetHashCode(byte[] obj) => obj.Aggregate(0, (current, b) => current ^ b.GetHashCode());
    }
}