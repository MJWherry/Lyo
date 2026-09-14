using Lyo.FileStorage.S3.Tests.Support;
using Lyo.IO.FileSystem;
using Lyo.IO.FileSystem.Tests;

namespace Lyo.FileStorage.S3.Tests;

public sealed class S3FileSystemContractTests : IFileSystemContractTests
{
    protected override IFileSystem Create()
    {
        var bucket = new InMemoryS3Bucket();
        return new S3FileSystem(bucket.CreateClient(), "bucket", "app-data");
    }
}
