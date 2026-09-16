using System.Net;
using System.Net.Http.Json;
using Lyo.Common.Core.Identifiers;
using Lyo.Music.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Music.Postgres.Tests;

public sealed class MusicCatalogHttpTests(MusicPostgresFixture fixture)
{
    [Fact]
    public async Task Search_EmptyFilterIds_ReturnsNone_AndLyricsPut_IsFoundBySearch()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IMusicStore>();
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(store);
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        app.MapPost("/Music/search", async (MusicReleaseQuery query, IMusicStore music, CancellationToken token) => Results.Ok(await music.SearchReleasesAsync(query, token)));
        app.MapPost("/Music/lyrics/search", async (MusicLyricsQuery query, IMusicStore music, CancellationToken token) => Results.Ok(await music.SearchLyricsAsync(query, token)));
        app.MapPut("/Music/track/{id:guid}/lyrics", async (Guid id, CatalogText text, IMusicStore music, CancellationToken token) => {
            await music.SaveTrackLyricsAsync(id, text.Text, token);
            return Results.NoContent();
        });
        await app.StartAsync(ct);
        try {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

            var empty = await client.PostAsJsonAsync("/Music/search", new MusicReleaseQuery { FilterIds = [] }, ct);
            empty.EnsureSuccessStatusCode();
            var none = await empty.Content.ReadFromJsonAsync<MusicRelease[]>(ct);
            Assert.NotNull(none);
            Assert.Empty(none);

            var tokenText = $"httplyric{LyoGuid.CreateV7():N}";
            var release = new MusicRelease {
                Title = tokenText,
                Slug = $"http-{LyoGuid.CreateV7():N}",
                Kind = MusicReleaseKind.Single,
                Status = MusicReleaseStatus.Released,
                Tracks = [new MusicTrack { DiscNumber = 1, TrackNumber = 1, Title = "Song" }]
            };
            await store.SaveReleaseAsync(release, ct);
            var put = await client.PutAsJsonAsync($"/Music/track/{release.Tracks[0].Id}/lyrics", new CatalogText { Text = $"line about {tokenText}" }, ct);
            Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

            var found = await client.PostAsJsonAsync("/Music/lyrics/search", new MusicLyricsQuery { FullText = tokenText }, ct);
            found.EnsureSuccessStatusCode();
            var hits = await found.Content.ReadFromJsonAsync<MusicLyricsHit[]>(ct);
            Assert.NotNull(hits);
            Assert.Contains(hits, h => h.TrackId == release.Tracks[0].Id);
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
