using System.Net;
using System.Net.Http.Json;
using Lyo.Common.Core.Identifiers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Audiobook.Postgres.Tests;

public sealed class AudiobookCatalogHttpTests(AudiobookPostgresFixture fixture)
{
    [Fact]
    public async Task Search_EmptyFilterIds_ReturnsNone_AndTranscriptPut_IsFoundBySearch()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IAudiobookStore>();
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(store);
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        app.MapPost(
            "/Audiobook/search", async (AudiobookQuery query, IAudiobookStore audiobooks, CancellationToken token) => Results.Ok(await audiobooks.SearchWorksAsync(query, token)));

        app.MapPost(
            "/Audiobook/transcripts/search",
            async (AudiobookTranscriptQuery query, IAudiobookStore audiobooks, CancellationToken token) => Results.Ok(await audiobooks.SearchTranscriptsAsync(query, token)));

        app.MapPut(
            "/Audiobook/chapter/{id:guid}/transcript", async (Guid id, CatalogText text, IAudiobookStore audiobooks, CancellationToken token) => {
                await audiobooks.SaveChapterTranscriptAsync(id, text.Text, token);
                return Results.NoContent();
            });

        await app.StartAsync(ct);
        try {
            using var client = new HttpClient { BaseAddress = new(app.Urls.First()) };
            var empty = await client.PostAsJsonAsync("/Audiobook/search", new AudiobookQuery { FilterIds = [] }, ct);
            empty.EnsureSuccessStatusCode();
            var none = await empty.Content.ReadFromJsonAsync<Audiobook[]>(ct);
            Assert.NotNull(none);
            Assert.Empty(none);
            var tokenText = $"httpchapter{LyoGuid.CreateV7():N}";
            var work = new Audiobook { Title = tokenText, Slug = $"http-{LyoGuid.CreateV7():N}", Chapters = [new() { Number = 1, Title = "Open" }] };
            await store.SaveWorkAsync(work, ct);
            var put = await client.PutAsJsonAsync($"/Audiobook/chapter/{work.Chapters[0].Id}/transcript", new CatalogText { Text = $"line about {tokenText}" }, ct);
            Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);
            var found = await client.PostAsJsonAsync("/Audiobook/transcripts/search", new AudiobookTranscriptQuery { FullText = tokenText }, ct);
            found.EnsureSuccessStatusCode();
            var hits = await found.Content.ReadFromJsonAsync<AudiobookTranscriptHit[]>(ct);
            Assert.NotNull(hits);
            Assert.Contains(hits, h => h.ChapterId == work.Chapters[0].Id);
        }
        finally {
            await app.StopAsync(ct);
            await app.DisposeAsync();
        }
    }

    private sealed class CatalogText
    {
        public string? Text { get; set; }
    }
}