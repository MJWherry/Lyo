using System.Security.Cryptography;
using Lyo.Encryption.Exceptions;
using Lyo.Keystore.Exceptions;
using KeyNotFoundException = Lyo.Keystore.Exceptions.KeyNotFoundException;

namespace Lyo.Encryption.Tests;

public class EncryptionExceptionTests
{
    [Fact]
    public void EncryptionException_DefaultConstructor_CreatesException()
    {
        var ex = new EncryptionException();
        Assert.NotNull(ex);
    }

    [Fact]
    public void EncryptionException_WithMessage_SetsMessage()
    {
        var message = "Test error message";
        var ex = new EncryptionException(message);
        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public void EncryptionException_WithInnerException_SetsInnerException()
    {
        var inner = new InvalidOperationException("Inner error");
        var ex = new EncryptionException("Outer error", inner);
        Assert.Equal("Outer error", ex.Message);
        Assert.Equal(inner, ex.InnerException);
    }

    [Fact]
    public void KeyNotFoundException_DefaultConstructor_CreatesException()
    {
        var ex = new KeyNotFoundException();
        Assert.NotNull(ex);
        Assert.Null(ex.KeyVersion);
    }

    [Fact]
    public void KeyNotFoundException_WithVersion_SetsVersion()
    {
        var ex = new KeyNotFoundException(42);
        Assert.Equal(42, ex.KeyVersion);
        Assert.Contains("42", ex.Message);
    }

    [Fact]
    public void KeyNotFoundException_WithVersionAndInner_SetsBoth()
    {
        var inner = new Exception("Inner");
        var ex = new KeyNotFoundException(42, inner);
        Assert.Equal(42, ex.KeyVersion);
        Assert.Equal(inner, ex.InnerException);
    }

    [Fact]
    public void KeyVersionNotFoundException_WithVersion_SetsVersion()
    {
        var ex = new KeyVersionNotFoundException(99);
        Assert.Equal(99, ex.KeyVersion);
        Assert.Contains("99", ex.Message);
    }

    [Fact]
    public void KeyVersionNotFoundException_WithVersionAndInner_SetsBoth()
    {
        var inner = new Exception("Inner");
        var ex = new KeyVersionNotFoundException(99, inner);
        Assert.Equal(99, ex.KeyVersion);
        Assert.Equal(inner, ex.InnerException);
    }

    [Fact]
    public void DecryptionFailedException_DefaultConstructor_HasDefaultMessage()
    {
        var ex = new DecryptionFailedException();
        Assert.NotNull(ex.Message);
        Assert.Contains("Decryption failed", ex.Message);
    }

    [Fact]
    public void DecryptionFailedException_WithMessage_SetsMessage()
    {
        var message = "Custom decryption error";
        var ex = new DecryptionFailedException(message);
        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public void DecryptionFailedException_WithInnerException_SetsInnerException()
    {
        var inner = new CryptographicException("Crypto error");
        var ex = new DecryptionFailedException("Decryption failed", inner);
        Assert.Equal("Decryption failed", ex.Message);
        Assert.Equal(inner, ex.InnerException);
    }

    [Fact]
    public void InvalidKeyException_DefaultConstructor_CreatesException()
    {
        var ex = new InvalidKeyException();
        Assert.NotNull(ex);
    }

    [Fact]
    public void InvalidKeyException_WithMessage_SetsMessage()
    {
        var message = "Key is invalid";
        var ex = new InvalidKeyException(message);
        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public void InvalidKeyException_WithInnerException_SetsInnerException()
    {
        var inner = new ArgumentException("Inner");
        var ex = new InvalidKeyException("Key invalid", inner);
        Assert.Equal("Key invalid", ex.Message);
        Assert.Equal(inner, ex.InnerException);
    }

    [Fact]
    public void EncryptionException_IsException()
    {
        var ex = new EncryptionException("Test");
        Assert.IsAssignableFrom<Exception>(ex);
    }

    [Fact]
    public void KeyNotFoundException_IsEncryptionException()
    {
        var ex = new KeyNotFoundException(1);
        Assert.IsAssignableFrom<EncryptionKeyException>(ex);
    }

    [Fact]
    public void KeyVersionNotFoundException_IsEncryptionException()
    {
        var ex = new KeyVersionNotFoundException(1);
        Assert.IsAssignableFrom<EncryptionKeyException>(ex);
    }

    [Fact]
    public void DecryptionFailedException_IsEncryptionException()
    {
        var ex = new DecryptionFailedException();
        Assert.IsAssignableFrom<EncryptionException>(ex);
    }

    [Fact]
    public void InvalidKeyException_IsEncryptionException()
    {
        var ex = new InvalidKeyException();
        Assert.IsAssignableFrom<EncryptionKeyException>(ex);
    }
}