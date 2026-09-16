namespace Lyo.FileManager.Tests;

public class FileManagerKeysTests
{
    [Fact]
    public void Ids_IncludesSavedKeysWhenTheLiveListIsEmpty()
    {
        var server = new FileManagerServer {
            Passphrase = "secret",
            Keys = [new() { Id = "1", Version = "1", Secret = "s" }]
        };
        var ids = FileManagerKeys.Ids(server, []);
        Assert.Equal([FileManagerLimits.LocalKeyId, "1"], ids);
    }

    [Fact]
    public void Resolve_OneKey_UsesItWhenTheChoiceIsBlank()
    {
        Assert.Equal("1", FileManagerKeys.Resolve(true, "  ", ["1"]));
    }

    [Fact]
    public void Resolve_TwoKeys_BlankChoice_ReturnsNull()
    {
        Assert.Null(FileManagerKeys.Resolve(true, null, ["a", "b"]));
    }

    [Fact]
    public void Resolve_TypedChoice_Wins()
    {
        Assert.Equal("kek", FileManagerKeys.Resolve(true, " kek ", ["1"]));
    }
}
