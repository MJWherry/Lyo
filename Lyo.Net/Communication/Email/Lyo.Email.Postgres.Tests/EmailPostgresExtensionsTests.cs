using Lyo.Email.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Email.Postgres.Tests;

public class EmailPostgresExtensionsTests
{
    private readonly EmailPostgresFixture _fixture;

    public EmailPostgresExtensionsTests(EmailPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public void AddEmailDbContext_WithNullServices_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => Extensions.AddEmailDbContext(null!, "Host=localhost"));
        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddEmailDbContext_WithNullConnectionString_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddEmailDbContext((string)null!));
        Assert.Equal("connectionString", ex.ParamName);
    }

    [Fact]
    public void AddEmailDbContext_WithEmptyConnectionString_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentException>(() => services.AddEmailDbContext(""));
        Assert.Equal("connectionString", ex.ParamName);
    }

    [Fact]
    public void AddEmailDbContextFactory_WithNullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddEmailDbContextFactory((Action<PostgresEmailOptions>)null!));
        Assert.Equal("configure", ex.ParamName);
    }

    [Fact]
    public void AddEmailDbContextFactory_WithNullConfiguration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddEmailDbContextFactoryFromConfiguration(null!));
        Assert.Equal("configuration", ex.ParamName);
    }

    [Fact]
    public async Task DbContext_CanConnectAndQuerySchema()
    {
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<EmailDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var canConnect = await context.Database.CanConnectAsync(TestContext.Current.CancellationToken);
        Assert.True(canConnect);
    }

    [Fact]
    public async Task DbContext_MigrationsApplied_SchemaExists()
    {
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<EmailDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var pending = await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken);
        Assert.Empty(pending);
    }

    [Fact]
    public async Task DbContext_CanInsertAndRetrieveOutboundLog_WithTextHtmlPathAndAttachments()
    {
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<EmailDbContext>>();
        var id = Guid.NewGuid();
        var sent = DateTime.UtcNow;
        var entity = new EmailLogEntity {
            Id = id,
            Direction = EmailDirection.Outbound,
            FromAddress = "sender@example.com",
            FromName = "Sender",
            ToAddressesJson = """["to@example.com"]""",
            Subject = "Hello",
            TextBody = "Plain text body",
            HtmlFileName = "hello.html",
            HtmlFilePath = "archive/hello.html",
            IsSuccess = true,
            Message = "250 OK",
            MessageId = "<id@example.com>",
            SentTimestamp = sent,
            Attachments = {
                new EmailAttachmentLogEntity {
                    FileName = "invoice.pdf",
                    ContentType = "application/pdf",
                    SizeBytes = 1024,
                    MetadataJson = """{"kind":"invoice"}""",
                    SortOrder = 0
                }
            }
        };

        await using (var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) {
            context.EmailLogs.Add(entity);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) {
            var retrieved = await context.EmailLogs.Include(e => e.Attachments).AsNoTracking()
                .SingleAsync(e => e.Id == id, TestContext.Current.CancellationToken);
            Assert.Equal(EmailDirection.Outbound, retrieved.Direction);
            Assert.Equal("sender@example.com", retrieved.FromAddress);
            Assert.Equal("Plain text body", retrieved.TextBody);
            Assert.Equal("hello.html", retrieved.HtmlFileName);
            Assert.Equal("archive/hello.html", retrieved.HtmlFilePath);
            Assert.True(retrieved.IsSuccess);
            Assert.Null(retrieved.ReceivedTimestamp);
            Assert.Single(retrieved.Attachments);
            var att = retrieved.Attachments.Single();
            Assert.Equal("invoice.pdf", att.FileName);
            Assert.Equal("application/pdf", att.ContentType);
            Assert.Equal(1024, att.SizeBytes);
            Assert.Contains("invoice", att.MetadataJson);
        }
    }

    [Fact]
    public async Task DbContext_CanInsertAndRetrieveInboundLog()
    {
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<EmailDbContext>>();
        var id = Guid.NewGuid();
        var received = DateTime.UtcNow;
        var entity = new EmailLogEntity {
            Id = id,
            Direction = EmailDirection.Inbound,
            FromAddress = "customer@example.com",
            ToAddressesJson = """["inbox@example.com"]""",
            Subject = "Question",
            TextBody = "Can you help?",
            IsSuccess = true,
            ReceivedTimestamp = received
        };

        await using (var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) {
            context.EmailLogs.Add(entity);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) {
            var retrieved = await context.EmailLogs.AsNoTracking().SingleAsync(e => e.Id == id, TestContext.Current.CancellationToken);
            Assert.Equal(EmailDirection.Inbound, retrieved.Direction);
            Assert.Equal("Can you help?", retrieved.TextBody);
            Assert.NotNull(retrieved.ReceivedTimestamp);
            Assert.Null(retrieved.HtmlFilePath);
        }
    }

    [Fact]
    public async Task DbContext_CanStoreFailedOutboundLog()
    {
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<EmailDbContext>>();
        var id = Guid.NewGuid();
        var entity = new EmailLogEntity {
            Id = id,
            Direction = EmailDirection.Outbound,
            ToAddressesJson = """["bad@example.com"]""",
            Subject = "Nope",
            IsSuccess = false,
            ErrorMessage = "SMTP connection failed"
        };

        await using (var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) {
            context.EmailLogs.Add(entity);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) {
            var retrieved = await context.EmailLogs.FindAsync([id], TestContext.Current.CancellationToken);
            Assert.NotNull(retrieved);
            Assert.False(retrieved.IsSuccess);
            Assert.Equal("SMTP connection failed", retrieved.ErrorMessage);
        }
    }

    [Fact]
    public void Model_HasNoFileStorageIdOrTemplateId()
    {
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<EmailDbContext>>();
        using var context = factory.CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(EmailAttachmentLogEntity));
        Assert.NotNull(entityType);
        Assert.Null(entityType.FindProperty("FileStorageId"));
        Assert.Null(entityType.FindProperty("TemplateId"));
        Assert.NotNull(entityType.FindProperty("SizeBytes"));
        Assert.NotNull(entityType.FindProperty("FileName"));
    }
}
