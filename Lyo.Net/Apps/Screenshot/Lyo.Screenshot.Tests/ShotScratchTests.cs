using Lyo.Common.Core.Identifiers;

namespace Lyo.Screenshot.Tests;

public class ShotScratchTests
{
    [Fact]
    public void Wipe_DeletesDirectoryAndFiles()
    {
        var directory = Path.Combine(Path.GetTempPath(), "lyo-shot-wipe-" + LyoGuid.CreateV7().ToString("N"));
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, "shot.png"), [1, 2, 3]);
        File.WriteAllBytes(Path.Combine(directory, "receipt-abc.bin"), [4, 5]);
        ShotScratch.Wipe(directory);
        Assert.False(Directory.Exists(directory));
    }

    [Fact]
    public void Wipe_MissingDirectory_DoesNothing()
    {
        var directory = Path.Combine(Path.GetTempPath(), "lyo-shot-missing-" + LyoGuid.CreateV7().ToString("N"));
        ShotScratch.Wipe(directory);
        Assert.False(Directory.Exists(directory));
    }
}