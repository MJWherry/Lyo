using Lyo.Keystore;
using Lyo.Keystore.Exceptions;

namespace Lyo.Encryption.Tests;

public class KeyValidatorTests
{
    [Fact]
    public void IsValidKeySize_CorrectSize_ReturnsTrue()
    {
        var key = new byte[32];
        Assert.True(KeyValidator.IsValidKeySize(key, 32));
    }

    [Fact]
    public void IsValidKeySize_WrongSize_ReturnsFalse()
    {
        var key = new byte[32];
        Assert.False(KeyValidator.IsValidKeySize(key, 16));
    }

    [Fact]
    public void IsValidKeySize_Null_ReturnsFalse() => Assert.False(KeyValidator.IsValidKeySize(null!, 32));

    [Fact]
    public void ValidateKeySizeOrThrow_CorrectSize_DoesNotThrow()
    {
        var key = new byte[32];
        KeyValidator.ValidateKeySizeOrThrow(key, 32);
    }

    [Fact]
    public void ValidateKeySizeOrThrow_WrongSize_Throws()
    {
        var key = new byte[32];
        Assert.Throws<InvalidKeyException>(() => KeyValidator.ValidateKeySizeOrThrow(key, 16));
    }

    [Fact]
    public void ValidateKeySizeOrThrow_Null_Throws() => Assert.Throws<ArgumentNullException>(() => KeyValidator.ValidateKeySizeOrThrow(null!, 32));

    [Fact]
    public void IsStrongKey_RandomKey_ReturnsTrue()
    {
        var key = SecureKeyGenerator.GenerateKey();
        Assert.True(KeyValidator.IsStrongKey(key));
    }

    [Fact]
    public void IsStrongKey_AllZeros_ReturnsFalse()
    {
        var key = new byte[32];
        Assert.False(KeyValidator.IsStrongKey(key));
    }

    [Fact]
    public void IsStrongKey_AllSameValue_ReturnsFalse()
    {
        var key = new byte[32];
        Array.Fill(key, (byte)42);
        Assert.False(KeyValidator.IsStrongKey(key));
    }

    [Fact]
    public void IsStrongKey_AlternatingPattern_ReturnsFalse()
    {
        var key = new byte[32];
        for (var i = 0; i < key.Length; i += 2) {
            key[i] = 1;
            key[i + 1] = 1;
        }

        Assert.False(KeyValidator.IsStrongKey(key));
    }

    [Fact]
    public void IsStrongKey_LowEntropy_ReturnsFalse()
    {
        // Create a key with only a few unique bytes
        var key = new byte[32];
        for (var i = 0; i < key.Length; i++)
            key[i] = (byte)(i % 4); // Only 4 unique values

        Assert.False(KeyValidator.IsStrongKey(key));
    }

    [Fact]
    public void IsStrongKey_Null_ReturnsFalse() => Assert.False(KeyValidator.IsStrongKey(null!));

    [Fact]
    public void IsStrongKey_Empty_ReturnsFalse() => Assert.False(KeyValidator.IsStrongKey([]));

    [Fact]
    public void ValidateKeyStrengthOrThrow_StrongKey_DoesNotThrow()
    {
        var key = SecureKeyGenerator.GenerateKey();
        KeyValidator.ValidateKeyStrengthOrThrow(key);
    }

    [Fact]
    public void ValidateKeyStrengthOrThrow_WeakKey_Throws()
    {
        var key = new byte[32]; // All zeros
        Assert.Throws<InvalidKeyException>(() => KeyValidator.ValidateKeyStrengthOrThrow(key));
    }

    [Fact]
    public void ValidateKeyStrengthOrThrow_Null_Throws() => Assert.Throws<ArgumentNullException>(() => KeyValidator.ValidateKeyStrengthOrThrow(null!));

    [Fact]
    public void ValidateKeyOrThrow_ValidKey_DoesNotThrow()
    {
        var key = SecureKeyGenerator.GenerateKey();
        KeyValidator.ValidateKeyOrThrow(key, 32);
    }

    [Fact]
    public void ValidateKeyOrThrow_WrongSize_Throws()
    {
        var key = SecureKeyGenerator.GenerateKey();
        Assert.Throws<InvalidKeyException>(() => KeyValidator.ValidateKeyOrThrow(key, 16));
    }

    [Fact]
    public void ValidateKeyOrThrow_WeakKey_Throws()
    {
        var key = new byte[32]; // All zeros
        Assert.Throws<InvalidKeyException>(() => KeyValidator.ValidateKeyOrThrow(key, 32));
    }

    [Fact]
    public void ValidateKeyOrThrow_Null_Throws() => Assert.Throws<ArgumentNullException>(() => KeyValidator.ValidateKeyOrThrow(null!, 32));

    [Fact]
    public void IsStrongKey_HighEntropy_ReturnsTrue()
    {
        var key = SecureKeyGenerator.GenerateKey();
        // Modify to ensure high entropy
        for (var i = 0; i < key.Length; i++)
            key[i] = (byte)(i % 256);

        Assert.True(KeyValidator.IsStrongKey(key));
    }
}