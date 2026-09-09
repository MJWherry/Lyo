namespace Lyo.FileStorage.S3.Tests;

public sealed class S3AwsCredentialHelpersTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "secret")]
    [InlineData("AKIAREAL", "   ")]
    [InlineData(null, "secret")]
    [InlineData("AKIAREAL", null)]
    public void TryGetExplicitCredentials_Missing_ReturnsFalse(string? accessKeyId, string? secretAccessKey)
    {
        Assert.False(S3AwsCredentialHelpers.TryGetExplicitCredentials(accessKeyId, secretAccessKey, out var credentials));
        Assert.Null(credentials);
    }

    [Fact]
    public void TryGetExplicitCredentials_RealKeys_ReturnsBasicCredentials()
    {
        Assert.True(S3AwsCredentialHelpers.TryGetExplicitCredentials("AKIAREALKEY", "super-secret", out var credentials));
        Assert.NotNull(credentials);
        Assert.Equal("AKIAREALKEY", credentials!.GetCredentials().AccessKey);
        Assert.Equal("super-secret", credentials.GetCredentials().SecretKey);
    }

    [Fact]
    public void TryGetExplicitCredentials_ReplaceMe_IsTreatedAsExplicit()
    {
        Assert.True(S3AwsCredentialHelpers.TryGetExplicitCredentials("replace-me", "replace-me", out var credentials));
        Assert.NotNull(credentials);
        Assert.Equal("replace-me", credentials!.GetCredentials().AccessKey);
    }

    [Fact]
    public void Resolve_NoKeysNoProfile_ReturnsNull()
        => Assert.Null(S3AwsCredentialHelpers.Resolve(null, null, null));

    [Fact]
    public void Resolve_NamedProfile_ReturnsProfileCredentials()
    {
        var credentialsFile = WriteTempCredentialsFile(
            """
            [work]
            aws_access_key_id = AKIAPROFILE
            aws_secret_access_key = profile-secret
            """);

        try {
            var credentials = S3AwsCredentialHelpers.Resolve(null, null, "work", credentialsFile);
            Assert.NotNull(credentials);
            var immutable = credentials.GetCredentials();
            Assert.Equal("AKIAPROFILE", immutable.AccessKey);
            Assert.Equal("profile-secret", immutable.SecretKey);
        }
        finally {
            File.Delete(credentialsFile);
        }
    }

    [Fact]
    public void Resolve_MissingProfile_Throws()
    {
        var credentialsFile = WriteTempCredentialsFile(
            """
            [other]
            aws_access_key_id = AKIOTHER
            aws_secret_access_key = other-secret
            """);

        try {
            var ex = Assert.Throws<InvalidOperationException>(() => S3AwsCredentialHelpers.Resolve(null, null, "work", credentialsFile));
            Assert.Contains("work", ex.Message);
        }
        finally {
            File.Delete(credentialsFile);
        }
    }

    [Fact]
    public void Resolve_ExplicitKeys_BeatProfile()
    {
        var credentialsFile = WriteTempCredentialsFile(
            """
            [work]
            aws_access_key_id = AKIAPROFILE
            aws_secret_access_key = profile-secret
            """);

        try {
            var credentials = S3AwsCredentialHelpers.Resolve("AKIAEXPLICIT", "explicit-secret", "work", credentialsFile);
            Assert.NotNull(credentials);
            var immutable = credentials.GetCredentials();
            Assert.Equal("AKIAEXPLICIT", immutable.AccessKey);
            Assert.Equal("explicit-secret", immutable.SecretKey);
        }
        finally {
            File.Delete(credentialsFile);
        }
    }

    private static string WriteTempCredentialsFile(string contents)
    {
        var path = Path.Combine(Path.GetTempPath(), "lyo-aws-creds-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(path, contents);
        return path;
    }
}