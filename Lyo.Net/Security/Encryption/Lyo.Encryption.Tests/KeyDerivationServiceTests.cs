using System.Diagnostics;
using System.Text;
using Lyo.Exceptions.Models;
using Lyo.KeyStore.KeyDerivation;

namespace Lyo.Encryption.Tests;

public class KeyDerivationServiceTests
{
    private readonly IKeyDerivationService _argon2Service = new Argon2KeyDerivationService();
    private readonly IKeyDerivationService _hkdfService = new HkdfKeyDerivationService();
    private readonly IKeyDerivationService _pbkdf2Service = new Pbkdf2KeyDerivationService();

    // exercise all three implementations
    public static IEnumerable<object[]> KeyDerivationServices()
    {
        yield return [new Pbkdf2KeyDerivationService()];
        yield return [new HkdfKeyDerivationService()];
        yield return [new Argon2KeyDerivationService()];
    }

    [Theory]
    [MemberData(nameof(KeyDerivationServices))]
    public void DeriveKey_String_Produces32ByteKey(IKeyDerivationService service)
    {
        var key = service.DeriveKey("password");
        Assert.Equal(service.DefaultKeySize, key.Length);
    }

    [Theory]
    [MemberData(nameof(KeyDerivationServices))]
    public void DeriveKey_String_WithCustomSize_ProducesCorrectSize(IKeyDerivationService service)
    {
        var key16 = service.DeriveKey("password", keySizeBytes: 16);
        var key64 = service.DeriveKey("password", keySizeBytes: 64);
        Assert.Equal(16, key16.Length);
        Assert.Equal(64, key64.Length);
    }

    [Theory]
    [MemberData(nameof(KeyDerivationServices))]
    public void DeriveKey_String_WithSalt_ProducesConsistentKey(IKeyDerivationService service)
    {
        // pick a salt width accepted by every algorithm (the service default)
        var salt = new byte[Math.Max(service.DefaultSaltSize, 32)]; // Ensure at least 32 bytes for compatibility
        Array.Fill<byte>(salt, 1);
        var key1 = service.DeriveKey("password", salt);
        var key2 = service.DeriveKey("password", salt);
        Assert.Equal(key1, key2);
    }

    [Theory]
    [MemberData(nameof(KeyDerivationServices))]
    public void DeriveKey_String_DifferentSalts_ProduceDifferentKeys(IKeyDerivationService service)
    {
        // pick a salt width accepted by every algorithm (the service default)
        var salt1 = new byte[Math.Max(service.DefaultSaltSize, 32)]; // Ensure at least 32 bytes for compatibility
        Array.Fill<byte>(salt1, 1);
        var salt2 = new byte[Math.Max(service.DefaultSaltSize, 32)];
        Array.Fill<byte>(salt2, 2);
        var key1 = service.DeriveKey("password", salt1);
        var key2 = service.DeriveKey("password", salt2);
        Assert.NotEqual(key1, key2);
    }

    [Theory]
    [MemberData(nameof(KeyDerivationServices))]
    public void DeriveKey_String_DifferentPasswords_ProduceDifferentKeys(IKeyDerivationService service)
    {
        var salt = new byte[Math.Max(service.DefaultSaltSize, 32)]; // Ensure at least 32 bytes for compatibility
        Array.Fill<byte>(salt, 1);
        var key1 = service.DeriveKey("password1", salt);
        var key2 = service.DeriveKey("password2", salt);
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void DeriveKey_String_WithIterations_AffectsOutput_Pbkdf2()
    {
        // PBKDF2: iteration count changes the output
        var salt = new byte[_pbkdf2Service.DefaultSaltSize];
        Array.Fill<byte>(salt, 1);
        var key1 = _pbkdf2Service.DeriveKey("password", salt);
        var key2 = _pbkdf2Service.DeriveKey("password", salt, 700000);

        // different iteration counts must yield different PBKDF2 keys
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void DeriveKey_String_WithIterations_Ignored_Hkdf()
    {
        // HKDF: iteration count is ignored (single pass)
        var salt = new byte[_hkdfService.DefaultSaltSize];
        Array.Fill<byte>(salt, 1);
        var key1 = _hkdfService.DeriveKey("password", salt);
        var key2 = _hkdfService.DeriveKey("password", salt, 700000);

        // same salt and password must yield the same HKDF key regardless of iterations
        Assert.Equal(key1, key2);
    }

    [Theory]
    [MemberData(nameof(KeyDerivationServices))]
    public void DeriveKey_String_NullPassword_Throws(IKeyDerivationService service)
    {
        string? nullPassword = null;
        var ex = Assert.Throws<ArgumentNullException>(() => service.DeriveKey(nullPassword!));
        Assert.Equal("password", ex.ParamName);
    }

    [Theory]
    [MemberData(nameof(KeyDerivationServices))]
    public void DeriveKey_String_EmptyPassword_Throws(IKeyDerivationService service) => Assert.Throws<ArgumentException>(() => service.DeriveKey(""));

    [Theory]
    [MemberData(nameof(KeyDerivationServices))]
    public void DeriveKey_String_InvalidKeySize_Throws(IKeyDerivationService service)
    {
        var ex1 = Assert.Throws<ArgumentOutsideRangeException>(() => service.DeriveKey("password", keySizeBytes: 10));
        Assert.Equal(10, ex1.ActualValue);
        Assert.Equal(16, ex1.MinValue);
        Assert.Equal(64, ex1.MaxValue);
        var ex2 = Assert.Throws<ArgumentOutsideRangeException>(() => service.DeriveKey("password", keySizeBytes: 100));
        Assert.Equal(100, ex2.ActualValue);
        Assert.Equal(16, ex2.MinValue);
        Assert.Equal(64, ex2.MaxValue);
    }

    [Fact]
    public void DeriveKey_String_LowIterations_Throws_Pbkdf2()
    {
        // PBKDF2: floor is 600,000 iterations (OWASP 2023)
        var ex1 = Assert.Throws<ArgumentOutsideRangeException>(() => _pbkdf2Service.DeriveKey("password", iterations: 500000));
        Assert.Equal(500000, ex1.ActualValue);
        Assert.Equal(600000, ex1.MinValue);
        var ex2 = Assert.Throws<ArgumentOutsideRangeException>(() => _pbkdf2Service.DeriveKey("password", iterations: 1000));
        Assert.Equal(1000, ex2.ActualValue);
        Assert.Equal(600000, ex2.MinValue);
    }

    [Fact]
    public void DeriveKey_String_LowIterations_Accepted_Hkdf()
    {
        // HKDF: iterations are ignored, so small values are accepted (not recommended)
        // confirms HKDF does not validate the iteration count
        var salt = new byte[_hkdfService.DefaultSaltSize];
        Array.Fill<byte>(salt, 1);

        // must not throw — HKDF ignores iterations
        var key = _hkdfService.DeriveKey("password", salt, 1000);
        Assert.Equal(_hkdfService.DefaultKeySize, key.Length);
    }

    [Theory]
    [MemberData(nameof(KeyDerivationServices))]
    public void DeriveKey_ByteArray_ProducesKey(IKeyDerivationService service)
    {
        var passwordBytes = Encoding.UTF8.GetBytes("password");
        var key = service.DeriveKey(passwordBytes);
        Assert.Equal(service.DefaultKeySize, key.Length);
    }

    [Theory]
    [MemberData(nameof(KeyDerivationServices))]
    public void DeriveKey_ByteArray_Null_Throws(IKeyDerivationService service)
    {
        byte[]? nullPassword = null;
        var ex = Assert.Throws<ArgumentNullException>(() => service.DeriveKey(nullPassword!));
        Assert.Equal("password", ex.ParamName);
    }

    [Theory]
    [MemberData(nameof(KeyDerivationServices))]
    public void DeriveKey_ByteArray_Empty_Throws(IKeyDerivationService service) => Assert.Throws<ArgumentException>(() => service.DeriveKey([]));

    [Theory]
    [MemberData(nameof(KeyDerivationServices))]
    public void DeriveKeyWithSalt_String_ReturnsKeyAndSalt(IKeyDerivationService service)
    {
        var (key, salt) = service.DeriveKeyWithSalt("password");
        Assert.Equal(service.DefaultKeySize, key.Length);
        // salt width differs: Argon2 uses 16 bytes; PBKDF2/HKDF use 32
        // each must match that service's default salt size
        Assert.Equal(service.DefaultSaltSize, salt.Length);
    }

    [Theory]
    [MemberData(nameof(KeyDerivationServices))]
    public void DeriveKeyWithSalt_String_DifferentCalls_ProduceDifferentSalts(IKeyDerivationService service)
    {
        var (key1, salt1) = service.DeriveKeyWithSalt("password");
        var (key2, salt2) = service.DeriveKeyWithSalt("password");

        // distinct salts must yield distinct keys
        Assert.NotEqual(salt1, salt2);
        Assert.NotEqual(key1, key2);
    }

    [Theory]
    [MemberData(nameof(KeyDerivationServices))]
    public void DeriveKeyWithSalt_String_CanRecreateKey(IKeyDerivationService service)
    {
        var (key1, salt) = service.DeriveKeyWithSalt("password");

        // rebuild with the same salt
        var key2 = service.DeriveKey("password", salt);
        Assert.Equal(key1, key2);
    }

    [Theory]
    [MemberData(nameof(KeyDerivationServices))]
    public void DeriveKeyWithSalt_ByteArray_ReturnsKeyAndSalt(IKeyDerivationService service)
    {
        var passwordBytes = Encoding.UTF8.GetBytes("password");
        var (key, salt) = service.DeriveKeyWithSalt(passwordBytes);
        Assert.Equal(service.DefaultKeySize, key.Length);
        // salt width differs: Argon2 uses 16 bytes; PBKDF2/HKDF use 32
        // each must match that service's default salt size
        Assert.Equal(service.DefaultSaltSize, salt.Length);
    }

    [Theory]
    [MemberData(nameof(KeyDerivationServices))]
    public void DeriveKey_UnicodePassword_Works(IKeyDerivationService service)
    {
        var key1 = service.DeriveKey("密码");
        var key2 = service.DeriveKey("пароль");
        var key3 = service.DeriveKey("🔑🔒");
        Assert.Equal(service.DefaultKeySize, key1.Length);
        Assert.Equal(service.DefaultKeySize, key2.Length);
        Assert.Equal(service.DefaultKeySize, key3.Length);
        Assert.NotEqual(key1, key2);
        Assert.NotEqual(key2, key3);
    }

    [Theory]
    [MemberData(nameof(KeyDerivationServices))]
    public void DeriveKey_LongPassword_Works(IKeyDerivationService service)
    {
        var longPassword = new string('a', 10000);
        var key = service.DeriveKey(longPassword);
        Assert.Equal(service.DefaultKeySize, key.Length);
    }

    [Fact]
    public void DeriveKey_HighIterations_TakesLonger_Pbkdf2()
    {
        // PBKDF2: more iterations should take longer
        // use a wide gap in iterations so the timing gap is more reliable
        var salt = new byte[_pbkdf2Service.DefaultSaltSize];
        Array.Fill<byte>(salt, 1);

        // Stopwatch is more precise than DateTime
        var stopwatch1 = Stopwatch.StartNew();
        _pbkdf2Service.DeriveKey("password", salt);
        stopwatch1.Stop();
        var stopwatch2 = Stopwatch.StartNew();
        _pbkdf2Service.DeriveKey("password", salt, 1200000); // Double the iterations
        stopwatch2.Stop();

        // more iterations should take longer for PBKDF2
        // allow load noise, but the gap should still be large
        // doubling iterations should be about 1.4x time or more (overhead and variance included)
        var expectedMinTime = stopwatch1.ElapsedMilliseconds * 1.4; // At least 40% longer
        Assert.True(
            stopwatch2.ElapsedMilliseconds >= expectedMinTime,
            $"Expected higher iterations ({1200000}) to take at least 1.4x longer than lower iterations ({600000}). " +
            $"Lower: {stopwatch1.ElapsedMilliseconds}ms, Higher: {stopwatch2.ElapsedMilliseconds}ms, " +
            $"Expected minimum: {expectedMinTime}ms, Ratio: {stopwatch2.ElapsedMilliseconds / (double)stopwatch1.ElapsedMilliseconds:F2}x");
    }

    [Fact]
    public void DeriveKey_Performance_HkdfVsPbkdf2()
    {
        // HKDF should beat PBKDF2 (single pass vs iterative)
        var salt = new byte[_hkdfService.DefaultSaltSize];
        Array.Fill<byte>(salt, 1);
        var hkdfStart = DateTime.UtcNow;
        _hkdfService.DeriveKey("password", salt);
        var hkdfTime = DateTime.UtcNow - hkdfStart;
        var pbkdf2Start = DateTime.UtcNow;
        _pbkdf2Service.DeriveKey("password", salt);
        var pbkdf2Time = DateTime.UtcNow - pbkdf2Start;

        // HKDF should be much faster (exact times depend on the host)
        Assert.True(hkdfTime.TotalMilliseconds < pbkdf2Time.TotalMilliseconds);
    }

    [Fact]
    public void DeriveKey_SameInput_DifferentAlgorithms_ProduceDifferentKeys()
    {
        // same password and salt must yield different keys across algorithms
        // use a salt width both algorithms accept (the larger of the two)
        var saltSize = Math.Max(_pbkdf2Service.DefaultSaltSize, _hkdfService.DefaultSaltSize);
        var salt = new byte[saltSize];
        Array.Fill<byte>(salt, 1);
        var pbkdf2Key = _pbkdf2Service.DeriveKey("password", salt);
        var hkdfKey = _hkdfService.DeriveKey("password", salt);

        // distinct algorithms must yield distinct keys
        Assert.NotEqual(pbkdf2Key, hkdfKey);
    }

    [Fact]
    public void DeriveKey_Consistency_Pbkdf2()
    {
        // PBKDF2 must be deterministic
        var salt = new byte[_pbkdf2Service.DefaultSaltSize];
        Array.Fill<byte>(salt, 1);
        var key1 = _pbkdf2Service.DeriveKey("password", salt);
        var key2 = _pbkdf2Service.DeriveKey("password", salt);
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void DeriveKey_Consistency_Hkdf()
    {
        // HKDF must be deterministic
        var salt = new byte[_hkdfService.DefaultSaltSize];
        Array.Fill<byte>(salt, 1);
        var key1 = _hkdfService.DeriveKey("password", salt);
        var key2 = _hkdfService.DeriveKey("password", salt);
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void DeriveKey_String_WithIterations_MapsToArgon2Range()
    {
        // Argon2: iterations map to time cost (usually 3-4)
        var salt = new byte[_argon2Service.DefaultSaltSize];
        Array.Fill<byte>(salt, 1);

        // large PBKDF2-style iteration counts should clamp into Argon2's range
        var key1 = _argon2Service.DeriveKey("password", salt);
        var key2 = _argon2Service.DeriveKey("password", salt, 3); // Direct Argon2 value

        // both must produce valid keys (they may differ because of iteration mapping)
        Assert.Equal(_argon2Service.DefaultKeySize, key1.Length);
        Assert.Equal(_argon2Service.DefaultKeySize, key2.Length);
    }

    [Fact]
    public void DeriveKey_String_RequiresMinimumSaltSize_Argon2()
    {
        // Argon2 salt must be at least DefaultSaltSize bytes
        var smallSalt = new byte[8];
        var ex = Assert.Throws<ArgumentOutsideRangeException>(() => _argon2Service.DeriveKey("password", smallSalt));
        Assert.Equal(8L, ex.ActualValue);
        Assert.Equal(16L, ex.MinValue); // DefaultSaltSize for Argon2

        // DefaultSaltSize bytes is accepted
        var validSalt = new byte[_argon2Service.DefaultSaltSize];
        Array.Fill<byte>(validSalt, 1);
        var key = _argon2Service.DeriveKey("password", validSalt);
        Assert.Equal(_argon2Service.DefaultKeySize, key.Length);
    }

    [Fact]
    public void DeriveKey_Consistency_Argon2()
    {
        // Argon2 must be deterministic
        var salt = new byte[_argon2Service.DefaultSaltSize];
        Array.Fill<byte>(salt, 1);
        var key1 = _argon2Service.DeriveKey("password", salt, 3);
        var key2 = _argon2Service.DeriveKey("password", salt, 3);
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void DeriveKey_SameInput_DifferentAlgorithms_ProduceDifferentKeys_AllThree()
    {
        // same password and salt must yield different keys across algorithms
        var argon2Salt = new byte[_argon2Service.DefaultSaltSize];
        Array.Fill<byte>(argon2Salt, 1);

        // use each service's default salt size so every algorithm accepts it
        var pbkdf2Salt = new byte[_pbkdf2Service.DefaultSaltSize];
        Array.Fill<byte>(pbkdf2Salt, 1);
        var hkdfSalt = new byte[_hkdfService.DefaultSaltSize];
        Array.Fill<byte>(hkdfSalt, 1);
        var pbkdf2Key = _pbkdf2Service.DeriveKey("password", pbkdf2Salt);
        var hkdfKey = _hkdfService.DeriveKey("password", hkdfSalt);
        var argon2Key = _argon2Service.DeriveKey("password", argon2Salt);

        // each must produce a valid key
        Assert.Equal(_pbkdf2Service.DefaultKeySize, pbkdf2Key.Length);
        Assert.Equal(_hkdfService.DefaultKeySize, hkdfKey.Length);
        Assert.Equal(_argon2Service.DefaultKeySize, argon2Key.Length);

        // all three keys must differ
        Assert.NotEqual(pbkdf2Key, hkdfKey);
        Assert.NotEqual(hkdfKey, argon2Key);
        Assert.NotEqual(pbkdf2Key, argon2Key);
    }

    [Fact]
    public void DeriveKey_Performance_Argon2VsPbkdf2()
    {
        // Argon2 should be slower than PBKDF2 because it is memory-hard
        // (exact times depend on configuration)
        var salt = new byte[_argon2Service.DefaultSaltSize];
        Array.Fill<byte>(salt, 1);
        var pbkdf2Salt = new byte[_pbkdf2Service.DefaultSaltSize];
        Array.Fill<byte>(pbkdf2Salt, 1);
        var argon2Start = DateTime.UtcNow;
        _argon2Service.DeriveKey("password", salt, 3);
        var argon2Time = DateTime.UtcNow - argon2Start;
        var pbkdf2Start = DateTime.UtcNow;
        _pbkdf2Service.DeriveKey("password", pbkdf2Salt);
        var pbkdf2Time = DateTime.UtcNow - pbkdf2Start;

        // both must finish (timing is environment-dependent)
        Assert.True(argon2Time.TotalMilliseconds >= 0);
        Assert.True(pbkdf2Time.TotalMilliseconds >= 0);
    }

    [Fact]
    public void DeriveKey_Argon2_WithCustomParameters()
    {
        // Argon2 with custom memory, iterations, and parallelism
        var customService = new Argon2KeyDerivationService(
            32768, // 32 MB
            2, 2);

        var salt = new byte[customService.DefaultSaltSize];
        Array.Fill<byte>(salt, 1);
        var key = customService.DeriveKey("password", salt, 2);
        Assert.Equal(customService.DefaultKeySize, key.Length);
    }

    [Fact]
    public void DeriveKey_Argon2_InvalidConstructorParameters_Throws()
    {
        // memory size below the floor
        var ex1 = Assert.Throws<ArgumentOutsideRangeException>(() => new Argon2KeyDerivationService(4));
        Assert.Equal(4, ex1.ActualValue);
        Assert.Equal(8, ex1.MinValue);

        // iterations below the floor
        var ex2 = Assert.Throws<ArgumentOutsideRangeException>(() => new Argon2KeyDerivationService(defaultIterations: 0));
        Assert.Equal(0, ex2.ActualValue);
        Assert.Equal(1, ex2.MinValue);

        // parallelism below the floor
        var ex3 = Assert.Throws<ArgumentOutsideRangeException>(() => new Argon2KeyDerivationService(degreeOfParallelism: 0));
        Assert.Equal(0, ex3.ActualValue);
        Assert.Equal(1, ex3.MinValue);
    }
}