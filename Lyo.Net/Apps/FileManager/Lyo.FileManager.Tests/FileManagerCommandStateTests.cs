using Lyo.Api.FileStorage.Models;

namespace Lyo.FileManager.Tests;

public class FileManagerCommandStateTests
{
    [Fact]
    public void From_DisabledServer_TurnsCommandsOff()
    {
        var state = FileManagerCommandState.From(FileStorageCapabilitiesResponse.None);
        Assert.False(state.PresignedRead);
        Assert.False(state.Copy);
        Assert.False(state.Move);
        Assert.False(state.AccessLinks);
        Assert.False(state.Encryption);
        Assert.False(state.CanEncrypt(true, 3));
    }

    [Fact]
    public void CanEncrypt_RequiresServerEncryptionAndAKey()
    {
        var state = FileManagerCommandState.From(new(true, true, true, true, true, true, true, true, true, true));
        Assert.True(state.PresignedRead);
        Assert.True(state.Copy);
        Assert.True(state.Move);
        Assert.True(state.AccessLinks);
        Assert.False(state.CanEncrypt(false, 0));
        Assert.True(state.CanEncrypt(true, 0));
        Assert.True(state.CanEncrypt(false, 1));
    }
}