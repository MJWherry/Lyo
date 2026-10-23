using Lyo.Common.Core.Identifiers;
using Lyo.ContactUs.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.ContactUs.Postgres.Tests;

[Trait("Category", "Fast")]
public sealed class PostgresContactUsServiceTests(ContactUsPostgresFixture fixture)
{
    [Fact]
    public async Task GetByIdAsync_AfterSubmit_ReturnsStoredFields()
    {
        var ct = TestCt;
        var service = fixture.ServiceProvider.GetRequiredService<IContactUsService>();
        var submitted = await service.SubmitAsync(
            Request("Ada Lovelace", "ada@example.com", "Question", "I have a question about your product.", "+1-555-0100", "Analytical Engines"), ct);

        Assert.True(submitted.IsSuccess);
        var loaded = await service.GetByIdAsync(submitted.SubmissionId!.Value, ct);
        Assert.NotNull(loaded);
        Assert.Equal(submitted.SubmissionId, loaded.Id);
        Assert.Equal("Ada Lovelace", loaded.Name);
        Assert.Equal("ada@example.com", loaded.Email);
        Assert.Equal("Question", loaded.Subject);
        Assert.Equal("I have a question about your product.", loaded.Message);
        Assert.Equal("+1-555-0100", loaded.Phone);
        Assert.Equal("Analytical Engines", loaded.Company);
        Assert.NotEqual(default, loaded.CreatedTimestamp);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var ct = TestCt;
        var service = fixture.ServiceProvider.GetRequiredService<IContactUsService>();
        var loaded = await service.GetByIdAsync(LyoGuid.CreateV7(), ct);
        Assert.Null(loaded);
    }

    [Fact]
    public async Task GetByIdAsync_EmptyId_Throws()
    {
        var service = fixture.ServiceProvider.GetRequiredService<IContactUsService>();
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByIdAsync(Guid.Empty, TestCt));
    }

    [Fact]
    public async Task ListAsync_ReturnsNewestFirst()
    {
        var ct = TestCt;
        await using (var context = await fixture.ContextFactory.CreateDbContextAsync(ct))
            await context.ContactSubmissions.ExecuteDeleteAsync(ct);

        var service = fixture.ServiceProvider.GetRequiredService<IContactUsService>();
        var older = await service.SubmitAsync(Request("Older", "older@example.com", "First", "This is the older message."), ct);
        await Task.Delay(TimeSpan.FromMilliseconds(20), ct);
        var newer = await service.SubmitAsync(Request("Newer", "newer@example.com", "Second", "This is the newer message."), ct);
        var listed = await service.ListAsync(ct);
        Assert.Equal(2, listed.Count);
        Assert.Equal(newer.SubmissionId, listed[0].Id);
        Assert.Equal(older.SubmissionId, listed[1].Id);
        Assert.True(listed[0].CreatedTimestamp > listed[1].CreatedTimestamp);
    }

    private static ContactUsRequest Request(string name, string email, string subject, string message, string? phone = null, string? company = null)
        => new() {
            Name = name,
            Email = email,
            Subject = subject,
            Message = message,
            Phone = phone,
            Company = company
        };
}