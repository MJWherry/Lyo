using System.Net;
using System.Net.Http.Json;
using Lyo.Common.Core.Identifiers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Podcast.Postgres.Tests;

public sealed class PodcastCatalogHttpTests(PodcastPostgresFixture fixture)
{
    [Fact]
    public async Task Search_EmptyFilterIds_ReturnsNone_AndTranscriptPut_IsFoundBySearch()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IPodcastStore>();
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(store);
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        app.MapPost(
            "/Podcast/search", async (PodcastShowQuery query, IPodcastStore podcasts, CancellationToken token) => Results.Ok(await podcasts.SearchShowsAsync(query, token)));

        app.MapPost(
            "/Podcast/transcripts/search",
            async (PodcastTranscriptQuery query, IPodcastStore podcasts, CancellationToken token) => Results.Ok(await podcasts.SearchTranscriptsAsync(query, token)));

        app.MapPut(
            "/Podcast/episode/{id:guid}/transcript", async (Guid id, CatalogText text, IPodcastStore podcasts, CancellationToken token) => {
                await podcasts.SaveEpisodeTranscriptAsync(id, text.Text, token);
                return Results.NoContent();
            });

        await app.StartAsync(ct);
        try {
            using var client = new HttpClient { BaseAddress = new(app.Urls.First()) };
            var empty = await client.PostAsJsonAsync("/Podcast/search", new PodcastShowQuery { FilterIds = [] }, ct);
            empty.EnsureSuccessStatusCode();
            var none = await empty.Content.ReadFromJsonAsync<PodcastShow[]>(ct);
            Assert.NotNull(none);
            Assert.Empty(none);
            var tokenText = $"httpep{LyoGuid.CreateV7():N}";
            var show = new PodcastShow {
                Title = tokenText, Slug = $"http-{LyoGuid.CreateV7():N}", Seasons = [new() { SeasonNumber = 1, Episodes = [new() { EpisodeNumber = 1, Title = "Pilot" }] }]
            };

            await store.SaveShowAsync(show, ct);
            var episodeId = show.Seasons[0].Episodes[0].Id;
            var put = await client.PutAsJsonAsync($"/Podcast/episode/{episodeId}/transcript", new CatalogText { Text = $"line about {tokenText}" }, ct);
            Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);
            var found = await client.PostAsJsonAsync("/Podcast/transcripts/search", new PodcastTranscriptQuery { FullText = tokenText }, ct);
            found.EnsureSuccessStatusCode();
            var hits = await found.Content.ReadFromJsonAsync<PodcastTranscriptHit[]>(ct);
            Assert.NotNull(hits);
            Assert.Contains(hits, h => h.EpisodeId == episodeId);
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