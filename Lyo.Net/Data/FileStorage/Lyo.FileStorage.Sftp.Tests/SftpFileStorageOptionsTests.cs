using Lyo.Sftp.Client;

namespace Lyo.FileStorage.Sftp.Tests;

public class SftpFileStorageOptionsTests
{
    [Fact]
    public void SectionName_IsSftpFileStorage() => Assert.Equal("SftpFileStorage", SftpFileStorageOptions.SectionName);

    [Fact]
    public void Sftp_NestedOptions_Validate()
    {
        var o = new SftpFileStorageOptions {
            Sftp = {
                Host = "localhost",
                Username = "u",
                Password = "p",
                HostKeyPolicy = SftpHostKeyPolicy.AcceptAny,
                RootRemoteDirectory = "/files"
            }
        };

        o.Sftp.Validate();
        Assert.Equal("/files", o.Sftp.RootRemoteDirectory);
    }
}