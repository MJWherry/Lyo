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
}