using Lyo.Diagnostic.StackTrace;
using Lyo.PackageMetadata;
using PackageMetadataDto = global::Lyo.PackageMetadata.PackageMetadata;

namespace Lyo.Diagnostic.Tests;

public sealed class StackTraceDecoderPackageMetadataStoreTests
{
    private static readonly Guid NpgsqlRowId = Guid.Parse("a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11");

    private const string TraceWithNpgsql = """
Npgsql.PostgresException: 23505
   at Npgsql.Internal.NpgsqlConnector.ReadMessage()
   at MyApp.Repos.OrderRepo.Insert(Order o) in OrderRepo.cs:line 18
""";

    [Fact]
    public void Decode_Throws_When_PackageMetadataStore_Configured()
    {
        var store = new InMemoryPackageMetadataStore();
        store.Register(["Npgsql."], new PackageMetadataDto(NpgsqlRowId, "Npgsql"));
        var decoder = new StackTraceDecoder(new StackTraceDecoderOptions { PackageMetadataStore = store });

        Assert.Throws<InvalidOperationException>(() => decoder.Decode(TraceWithNpgsql));
    }

    [Fact]
    public async Task DecodeAsync_With_Store_Enriches_Frames()
    {
        var store = new InMemoryPackageMetadataStore();
        var meta = new PackageMetadataDto(
            NpgsqlRowId,
            "Npgsql",
            Version: "8.0.0",
            PackageFileSha512Hex:
            "cf83e1357eefb8bdf1542850d66d8007d620e4050b5715dc83f4a921d36ce9ce47d0d13c5d85f2b0ff8318d2877eec2f63b931bd47417a81a538327af927da3e",
            ProjectUrl: "https://github.com/npgsql/npgsql");
        store.Register(["Npgsql."], meta);
        var decoder = new StackTraceDecoder(new StackTraceDecoderOptions { PackageMetadataStore = store });

        var decoded = await decoder.DecodeAsync(TraceWithNpgsql, TestContext.Current.CancellationToken);

        Assert.NotNull(decoded.AllFrames[0].PackageMetadata);
        Assert.Equal(NpgsqlRowId, decoded.AllFrames[0].PackageMetadata!.Id);
        Assert.Equal("Npgsql", decoded.AllFrames[0].PackageMetadata!.Name);
        Assert.Equal("8.0.0", decoded.AllFrames[0].PackageMetadata!.Version);
        Assert.Equal(
            "cf83e1357eefb8bdf1542850d66d8007d620e4050b5715dc83f4a921d36ce9ce47d0d13c5d85f2b0ff8318d2877eec2f63b931bd47417a81a538327af927da3e",
            decoded.AllFrames[0].PackageMetadata!.PackageFileSha512Hex);
        Assert.Null(decoded.AllFrames[1].PackageMetadata);
    }

    [Fact]
    public async Task DecodeAsync_Classification_Matches_Decode_Without_Store()
    {
        var store = new InMemoryPackageMetadataStore();
        store.Register(["Npgsql."], new PackageMetadataDto(NpgsqlRowId, "Npgsql"));
        var withStore = new StackTraceDecoder(new StackTraceDecoderOptions { PackageMetadataStore = store });
        var plain = new StackTraceDecoder();

        var a = await withStore.DecodeAsync(TraceWithNpgsql, TestContext.Current.CancellationToken);
        var b = plain.Decode(TraceWithNpgsql);

        Assert.Equal(b.AllFrames.Select(f => f.Category).ToList(), a.AllFrames.Select(f => f.Category).ToList());
        Assert.Equal(b.UserFrameCount, a.UserFrameCount);
        Assert.Equal(b.Fingerprint, a.Fingerprint);
    }

    [Fact]
    public async Task DecodeAsync_With_Store_Calls_Bulk_Resolve_Once_Per_Decode_Block()
    {
        var inner = new InMemoryPackageMetadataStore();
        inner.Register(["Npgsql."], new PackageMetadataDto(NpgsqlRowId, "Npgsql"));
        var counting = new CountingPackageMetadataStore(inner);
        var decoder = new StackTraceDecoder(new StackTraceDecoderOptions { PackageMetadataStore = counting });

        var trace = """
Npgsql.PostgresException: 23505
   at Npgsql.Internal.NpgsqlConnector.ReadMessage()
   at Npgsql.Internal.NpgsqlConnector.ReadMessage()
   at MyApp.Repos.OrderRepo.Insert(Order o) in OrderRepo.cs:line 18
""";

        _ = await decoder.DecodeAsync(trace, TestContext.Current.CancellationToken);

        Assert.Equal(1, counting.TryGetManyCallCount);
        Assert.Equal(0, counting.TryGetSingleCallCount);
    }

    [Fact]
    public async Task DecodeAsync_With_Store_Embedded_Inner_Block_Single_TryMany()
    {
        var innerStore = new InMemoryPackageMetadataStore();
        innerStore.Register(["Npgsql."], new PackageMetadataDto(NpgsqlRowId, "Npgsql"));
        var counting = new CountingPackageMetadataStore(innerStore);
        var decoder = new StackTraceDecoder(new StackTraceDecoderOptions { PackageMetadataStore = counting });

        const string traceWithEmbeddedInner =
            """
Npgsql.PostgresException: 23505
   at MyApp.Inner.Deepest() in Inner.cs:line 1
 ---> System.IO.FileNotFoundException: missing
   at Npgsql.Internal.NpgsqlConnector.ReadMessage()
   --- End of inner exception stack trace ---
""";

        _ = await decoder.DecodeAsync(traceWithEmbeddedInner, TestContext.Current.CancellationToken);

        Assert.Equal(1, counting.TryGetManyCallCount);
    }

    [Fact]
    public async Task DecodeAsync_Exception_With_Chained_Inner_Single_TryMany()
    {
        var outer = NestedFailureWithInner();

        var innerStore = new InMemoryPackageMetadataStore();
        innerStore.Register(["Npgsql."], new PackageMetadataDto(NpgsqlRowId, "Npgsql"));
        var counting = new CountingPackageMetadataStore(innerStore);
        var decoder = new StackTraceDecoder(new StackTraceDecoderOptions { PackageMetadataStore = counting });

        _ = await decoder.DecodeAsync(outer, TestContext.Current.CancellationToken);

        Assert.Equal(1, counting.TryGetManyCallCount);
    }

    private static InvalidOperationException NestedFailureWithInner()
    {
        try {
            ThrowsIo();
        }
        catch (IOException ex) {
            return new InvalidOperationException("wrapped", ex);
        }

        throw new System.Diagnostics.UnreachableException();
    }

    private static void ThrowsIo()
    {
        throw new IOException("noise");
    }

    private sealed class CountingPackageMetadataStore : IPackageMetadataStore
    {
        private readonly IPackageMetadataStore _inner;

        internal CountingPackageMetadataStore(IPackageMetadataStore inner)
        {
            _inner = inner;
        }

        internal int TryGetManyCallCount { get; private set; }

        internal int TryGetSingleCallCount { get; private set; }

        public ValueTask<PackageMetadataDto?> TryGetForFrameAsync(string namespacePrefix, string strippedMethodPrefix, CancellationToken cancellationToken = default)
        {
            TryGetSingleCallCount++;
            return _inner.TryGetForFrameAsync(namespacePrefix, strippedMethodPrefix, cancellationToken);
        }

        public ValueTask<IReadOnlyDictionary<string, PackageMetadataDto?>> TryGetManyForStrippedMethodPrefixesAsync(IReadOnlyList<string> strippedMethodPrefixes,
            CancellationToken cancellationToken = default)
        {
            TryGetManyCallCount++;
            return _inner.TryGetManyForStrippedMethodPrefixesAsync(strippedMethodPrefixes, cancellationToken);
        }

        public Task RegisterManyAsync(IReadOnlyList<PackageMetadataRegistration> registrations, CancellationToken cancellationToken = default)
            => _inner.RegisterManyAsync(registrations, cancellationToken);
    }
}
