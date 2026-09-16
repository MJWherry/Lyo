using System.Net;
using System.Net.Http.Json;
using Lyo.Common.Core.Identifiers;
using Lyo.Story.Enums;
using Lyo.Story.Models.Request;
using Lyo.Story.Models.Response;

namespace Lyo.Story.Api.Tests;

public sealed class StoryRouteSecurityTests(StoryApiFixture fixture)
{
    [Fact]
    public async Task StoryPlay_OtherPlayersPlaythrough_Returns403()
    {
        var story = await SaveStoryAsync(null);
        var state = await StartAsync(NewSubject(), story.Id, null);
        using var other = fixture.CreateHttp(NewSubject());
        var id = state.Playthrough.Id;

        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"Story/playthrough/{id}", TestCt)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"Story/playthrough/{id}/moves", TestCt)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"Story/playthrough/{id}/events", TestCt)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"Story/playthrough/{id}/causes/gold", TestCt)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsync($"Story/playthrough/{id}/advance", null, TestCt)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsJsonAsync($"Story/playthrough/{id}/choose", new StoryChooseReq { ChoiceKey = "x" }, TestCt)).StatusCode);
    }

    [Fact]
    public async Task StoryPlay_OwnPlaythrough_Returns200()
    {
        var story = await SaveStoryAsync(null);
        var subject = NewSubject();
        var state = await StartAsync(subject, story.Id, null);
        using var http = fixture.CreateHttp(subject);

        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync($"Story/playthrough/{state.Playthrough.Id}", TestCt)).StatusCode);
    }

    [Fact]
    public async Task StoryStart_BodyPlayerRef_UsesClaims()
    {
        var story = await SaveStoryAsync(null);
        var subject = NewSubject();
        using var http = fixture.CreateHttp(subject);

        var response = await http.PostAsJsonAsync("Story/playthrough/start", new StoryStartPlaythroughReq { StoryId = story.Id, PlayerRef = "player:someone-else" }, TestCt);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("player:" + subject, (await response.Content.ReadFromJsonAsync<StoryPlayStateRes>(TestCt))?.Playthrough.PlayerRef);
    }

    [Fact]
    public async Task StorySetVariables_PlayerCaller_Returns403()
    {
        var story = await SaveStoryAsync(null);
        var subject = NewSubject();
        var state = await StartAsync(subject, story.Id, null);
        using var player = fixture.CreateHttp(subject);
        using var author = fixture.CreateHttp(NewSubject(), StoryApiFixture.AuthorRole);
        var body = new StorySetVariablesReq { Values = { ["gold"] = "99" } };

        var denied = await player.PostAsJsonAsync($"Story/playthrough/{state.Playthrough.Id}/variables", body, TestCt);
        var allowed = await author.PostAsJsonAsync($"Story/playthrough/{state.Playthrough.Id}/variables", body, TestCt);

        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task StoryGetById_OtherTenant_Returns404()
    {
        var tenant = LyoGuid.CreateV7();
        var story = await SaveStoryAsync(tenant);
        using var outsider = fixture.CreateHttp(NewSubject(), tenant: LyoGuid.CreateV7());
        using var insider = fixture.CreateHttp(NewSubject(), tenant: tenant);

        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"Story/graph/{story.Id}", TestCt)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"Story/graph/by-slug/{story.Slug}", TestCt)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PostAsJsonAsync("Story/playthrough/start", new StoryStartPlaythroughReq { StoryId = story.Id }, TestCt)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await insider.GetAsync($"Story/graph/{story.Id}", TestCt)).StatusCode);
    }

    [Fact]
    public async Task StoryError_ReturnsProblemDetails()
    {
        var story = await SaveStoryAsync(null);
        var subject = NewSubject();
        var state = await StartAsync(subject, story.Id, null);
        using var http = fixture.CreateHttp(subject);

        var response = await http.PostAsJsonAsync($"Story/playthrough/{state.Playthrough.Id}/choose", new StoryChooseReq { ChoiceKey = "missing" }, TestCt);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static string NewSubject() => "p-" + LyoGuid.CreateV7().ToString("N");

    private async Task<StoryGraphRes> SaveStoryAsync(Guid? tenant)
    {
        using var author = fixture.CreateHttp(NewSubject(), StoryApiFixture.AuthorRole, tenant);
        var slug = "s-" + LyoGuid.CreateV7().ToString("N");
        var graph = new StoryGraphReq {
            Title = "Test",
            Slug = slug,
            TenantId = tenant,
            Status = StoryStatus.Published,
            StartNodeKey = "start",
            Nodes = [new() { Key = "start", Body = "Hello" }],
            Variables = [new() { Key = "gold", Type = StoryVariableType.Integer, DefaultValue = "0" }]
        };

        var response = await author.PostAsJsonAsync("Story/graph", graph, TestCt);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<StoryGraphRes>(TestCt))!;
    }

    private async Task<StoryPlayStateRes> StartAsync(string subject, Guid storyId, Guid? tenant)
    {
        using var http = fixture.CreateHttp(subject, tenant: tenant);
        var response = await http.PostAsJsonAsync("Story/playthrough/start", new StoryStartPlaythroughReq { StoryId = storyId }, TestCt);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<StoryPlayStateRes>(TestCt))!;
    }
}
