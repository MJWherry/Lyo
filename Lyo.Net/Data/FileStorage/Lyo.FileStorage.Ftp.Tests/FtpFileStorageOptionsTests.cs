using Lyo.Ftp.Client;

namespace Lyo.FileStorage.Ftp.Tests;

public class FtpFileStorageOptionsTests
{
    [Fact]
    public void SectionName_IsFtpFileStorage() => Assert.Equal("FtpFileStorage", FtpFileStorageOptions.SectionName);

    [Fact]
    public void Ftp_NestedOptions_Validate()
    {
        var o = new FtpFileStorageOptions {
            Ftp = {
                Host = "localhost",
                Username = "u",
                Password = "p",
                RootRemoteDirectory = "/files",
                EncryptionMode = FtpEncryptionMode.None
            }
        };

        o.Ftp.Validate();
        Assert.Equal("/files", o.Ftp.RootRemoteDirectory);
    }
}