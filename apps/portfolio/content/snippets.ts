export const snippets = {
  querySetup: `builder.Services.AddLocalCache(); // or AddFusionCache()
builder.Services.AddLyoQueryServices();
builder.Services.AddLyoCrudServices<MyDbContext>();
builder.Services.AddDbContextFactory<MyDbContext>(...);
builder.Services.AddScoped<ILyoMapper, MapsterLyoMapper>();

// Typed endpoints: QueryConcrete / QueryProject / CRUD / Patch / Bulk / Export
app.CreateBuilder<Person, PersonReq, PersonRes>("Person")
    .WithCrud(ApiFeatureSet.All)
    .Map();`,

  queryConcrete: `// Typed concrete query against Person
var req = new QueryConcreteReqBuilder()
    .Start(0).Amount(25)
    .Options(o => o.TotalCountMode = TotalCountMode.Exact)
    .Where(w => w
        .And(a => a
            .NotNull("FirstName")
            .NotNull("LastName")
            .GreaterThan("CreatedAt", DateTime.UtcNow.AddYears(-1))))
    .Sort("LastName")
    .Build();

var page = await personApi.QueryConcreteAsync(req, ct);
// page.Total populated when TotalCountMode = Exact`,

  queryProject: `// Projection: select fields + optional computed columns
var proj = new ProjectionQueryReqBuilder()
    .Start(0).Amount(50)
    .Select("Id", "FirstName", "LastName", "SourceEntityType")
    .Computed("DisplayName", "{FirstName} {LastName}")
    .Build();

var rows = await personApi.QueryProjectAsync(proj, ct);
// rows.EntityTypes describes nested graphs when includes are used`,

  queryRoot: `// Root From/Joins sparse query: POST /Query
var root = new QueryReqBuilder()
    .From("p", "Person")
    .LeftJoin("c", "Contact", on => on.From("p.Id").To("c.PersonId"))
    .Select("p.Id", "p.LastName", "c.Email")
    .Amount(100)
    .Build();`,

  queryPatchBulk: `// Property-level patch (optional PatchPropertyAuthorization allowlists)
await personApi.PatchAsync(new PatchRequest
{
    Keys = [[id]],
    Properties = new() { ["LastName"] = "Updated" },
}, ct);

// Bulk create/update — batch first, individual fallback on failure
var bulk = await personApi.BulkCreateAsync(items, ct);
// bulk.Succeeded / Failed with per-item errors`,

  jobRegisterApi: `// API host: schema + HTTP surface + event publish
services.AddPostgresJobManagement(o =>
{
    o.ConnectionString = connStr;
    o.EnableAutoMigrations = true;
});
services.AddMqJobEventPublisher();
services.AddJobMaintenanceServiceFromConfiguration(configuration);
// app.BuildJobGroup(); // maps Job/Definition, Schedule, Run, …`,

  jobDefinition: `var def = JobDefinitionBuilder.New("Nightly Sync")
    .ForCSharpWorker()
    .SetType("Import")
    .AddJobParameter("BatchSize", JobParameterType.Int, 500)
    .WithRetry(maxAttempts: 3, backoff: JobRetryBackoff.Exponential)
    .WithSla(TimeSpan.FromMinutes(30))
    .Build();

var schedule = JobScheduleBuilder.EveryDay()
    .SetTimes(2, 0)
    .WithMisfirePolicy(JobMisfirePolicy.RunOnce)
    .Build();`,

  jobScheduler: `// Scheduler host: poll definitions → create runs → MQ trigger
services.AddJobClient(sp => sp.GetRequiredService<IApiClient>());
services.AddMqJobEventPublisherFromConfiguration(configuration);
services.AddJobScheduler(); // JobSchedulerOptions / polling interval
// optional: services.AddJobWorkflowEngine();`,

  jobWorker: `public sealed class ImportWorker : JobWorkerBase
{
    protected override async Task ExecuteAsync(IJobWorkerContext ctx, CancellationToken ct)
    {
        await ctx.ReportProgressAsync(10, "Starting", ct);
        var batch = ctx.Run.JobRunParameters.GetInt("BatchSize") ?? 100;
        // … do work …
        ctx.Results.AddCreateCount(batch);
        await ctx.ReportProgressAsync(100, "Done", ct);
    }
}

// services.AddJobWorker<ImportWorker>(workerType: "csharp", apiBaseUrl: "...");`,

  jobSignalR: `services.AddJobSignalR();
app.MapJobHub(); // /hubs/job → JobEvent
// run.created | started | finished | cancelled | alert | definition.updated

// Blazor: <JobManagement BaseRoute="Job" /> + HubConnection.On("JobEvent", …)`,

  jobAlerts: `services.AddJobAlerts(o =>
{
    o.WebhookUrl = configuration["JobAlerts:Webhook"];
});
// JobAlertConsumer listens on job.notifications.alert
// and fans out via INotificationPublisher / webhook`,

  fileStorage: `await fileStorage.SaveFileAsync(new SaveFileRequest
{
    Key = "reports/q1.bin",
    Content = stream,
    ContentType = "application/octet-stream",
    Compress = true,
    Encrypt = true,
}, ct);`,

  fileStorageStage: `// Staged upload for large payloads
var stage = await staged.BeginAsync(new BeginStageRequest
{
    Key = "uploads/big.bin",
    ContentType = "application/octet-stream",
}, ct);
await stage.WriteAsync(chunk, ct);
await staged.CompleteAsync(stage.SessionId, ct);`,

  encryption: `var enc = new AesGcmEncryptionService(keyStore);
await using var sealed = await enc.EncryptAsync(plain, ct);
// Probe headers without decrypting via EncryptionHeader.Read`,

  encryptionTwoKey: `// DEK encrypted under KEK — envelope for at-rest files
var twoKey = new TwoKeyEncryptionService<AesGcm, AesGcm>(dekStore, kekStore);
await using var envelope = await twoKey.EncryptAsync(plain, ct);
// FileStorage Encrypt=true uses the same pipeline on SaveFileAsync`,

  encryptionKeyed: `const string keyName = "primary";
services.AddKeyedLocalKeyStore(keyName, store =>
    store.UpdateKeyFromString("default-key", config["Encryption:KekSecret"]!));
services.AddEncryptionServiceKeyed(keyName, keyStoreName: keyName);
// Mixed DEK/KEK algorithms:
// services.AddEncryptionServiceKeyed<XChaCha20Poly1305EncryptionService, AesGcmEncryptionService>(keyName, keyName);

var envelope = sp.GetRequiredKeyedService<ITwoKeyEncryptionService>(keyName);`,

  encryptionRsa: `services.AddRsaEncryption(publicPemPath: "keys/public.pem", privatePemPath: "keys/private.pem");
services.AddAesGcmRsaEncryption(publicPemPath: "keys/public.pem", privatePemPath: "keys/private.pem");
// Hybrid: RSA wraps a content key; AES-GCM seals the payload`,

  compression: `services.AddCompressionService(o => o.DefaultAlgorithm = CompressionAlgorithm.Zstd);
var result = await compression.CompressAsync(input, ct);
// result.Ratio / SpaceSavedPercent available for telemetry`,

  compressionBytes: `var service = new CompressionService();
var compressInfo = service.Compress(payload, out var compressed);
Console.WriteLine($"ratio={compressInfo.CompressionRatio:P2}");
var decompressInfo = service.Decompress(compressed, out var roundTrip);
// Bomb protection: MaxDecompressedSize on options`,

  compressionStream: `await using var limited = new MaxLengthStream(output, maxBytes: 50_000_000);
await compression.CompressAsync(
    input,
    limited,
    algorithm: CompressionAlgorithm.LZ4,
    ct);`,

  compressionResolver: `// Per-algorithm dispatch (FileStorage uses this for metadata-driven decompress)
var resolver = compression.Resolver;
await resolver.CompressAsync(CompressionAlgorithm.Zstd, input, output, ct);
await resolver.DecompressAsync(CompressionAlgorithm.Zstd, input, output, ct);`,

  tempIo: `services.AddIOTempService();
await using var session = temp.CreateSession();
var path = await session.CreateFileAsync(chunkBytes);
await File.WriteAllBytesAsync(session.GetFilePath("report.pdf"), reportBytes, ct);
// Session dispose cleans up files + dirs under size policy`,

  tempIoGenerator: `// Random / structured fixtures for tests and load harnesses
var file = session.Generator.CreateRandomFile(FileSizeUnitInfo.Megabyte, 1);
var csv  = session.Generator.CreateCsvFile(rows: 500, columns: 10);
var json = session.Generator.CreateJsonFile(depth: 3, keysPerObject: 5);
var zip  = session.Generator.CreateZipFile(TempDirectorySpec.Flat(10, 1024));`,

  tempIoOptions: `services.AddIOTempService(o =>
{
    o.DirectoryName = "my-app-temp";
    o.MaxTotalSizeBytes = 500 * 1024 * 1024;
    o.OverflowHandling = OverflowHandling.DeleteOldest;
});
services.AddIOTempServiceWithAutoCleanup(
    cleanupInterval: TimeSpan.FromHours(1),
    initialDelay: TimeSpan.FromMinutes(5));`,

  tempIoSpec: `var spec = TempDirectorySpec.Builder()
    .WithFiles(5, FileSizeUnitInfo.Kilobyte, 4)
    .WithSubdirectory(sub => sub.WithFiles(3, 256))
    .Build();
var dir = session.Generator.SimulateDirectory(spec);`,

  fileSystemWatcher: `using var watcher = new FileSystemWatcher(@"C:\\ingest");
watcher.FileCreated += (_, e) => Console.WriteLine($"created {e.NewPath}");
watcher.FileMoved   += (_, e) => Console.WriteLine($"{e.OldPath} -> {e.NewPath}");
watcher.OnAnyChange += (_, e) => Console.WriteLine($"{e.ChangeType}: {e.NewPath ?? e.OldPath}");
// Keep host alive while snapshots + debounce run`,

  fileSystemWatcherOptions: `var options = new FileSystemWatcherOptions
{
    IncludeSubdirectories = true,
    DebounceTimerDelay = 500,
    EnableFileHashing = true,   // move/rename via content hash
    EnableMetrics = true,
};
using var watcher = new FileSystemWatcher(path, options, logger, metrics);
watcher.Error += (_, ex) => logger.LogError(ex, "watcher fault");`,

  reportingRegister: `services.AddLyoQueryServices();
services.AddLocalCache();
services.AddIOTempService();
services.AddPostgresReportingManagement(o =>
{
    o.ConnectionString = cs;
    o.EnableAutoMigrations = true;
    o.AllowAdHocGeneration = true;
    o.MaxConcurrentGenerations = 4; // ReportBusyException → 503
    o.GenerationRetention = TimeSpan.FromDays(90);
});
services.AddReportingWebRenderer(); // HTML/PDF
services.AddLyoApiReporting();
app.BuildReportingGroup(new ReportingApiOptions { /* auth */ });`,

  reportingHooks: `services.AddReportingGenerationHooks(new ReportGenerationHooks
{
    AfterRenderAsync = async (ctx, ct) =>
    {
        var saved = await storage.SaveFileAsync(
            ctx.StagedFilePath!, ctx.FileName,
            pathPrefix: ctx.PathPrefix, contentType: ctx.ContentType, ct: ct);
        ctx.OutputFileId = saved.Id;
    },
    OnCleanupAsync = async (ctx, ct) =>
    {
        if (ctx.OutputFileId is Guid id)
            await storage.DeleteFileAsync(id, ct: ct);
    },
});`,

  cache: `var value = await cache.GetOrSetAsync(
    "person:stats",
    () => LoadStatsAsync(ct),
    new CacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
    ct);`,

  lockSnippet: `await using (await locks.AcquireAsync("report:generate", ct))
{
    await GenerateReportAsync(ct);
}

// Keyed semaphore for N-way concurrency per key
await using var _ = await semaphores.WaitAsync("ingest", ct);`,

  privacy: `var redactor = privacy.CreateRedactor(PrivacyPreset.PiiStrict);
var safe = redactor.RedactText(logLine);
var jsonSafe = redactor.RedactJson(payload);`,

  resilience: `services.AddLyoResilienceFromConfiguration(configuration);
// Polly pipelines bound by name for HttpClient / DB calls
builder.Services.AddHttpClient("vendor")
    .AddResilienceHandler("vendor-default");`,

  bffTs: `import { createAsyncApiClient } from "lyo-api-client";
import { createAsyncPersonApiClient, baselineQuery } from "lyo-person-api-client";

const api = createAsyncApiClient({
  baseUrl: process.env.LYO_API_BASE_URL!,
  transport: fetchTransport,
});
const personApi = createAsyncPersonApiClient(api);
const res = await personApi.queryPerson(baselineQuery({ amount: 10 }));`,
} as const;
