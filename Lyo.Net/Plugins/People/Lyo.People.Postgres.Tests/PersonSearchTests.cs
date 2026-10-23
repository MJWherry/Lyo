using Lyo.People.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.People.Postgres.Tests;

public sealed class PersonSearchTests(PeoplePostgresFixture fixture)
{
    [Fact]
    public async Task Search_Person_MatchesDisplayName()
    {
        var factory = fixture.ServiceProvider.GetRequiredService<IDbContextFactory<PeopleDbContext>>();
        var store = new PostgresPeopleStore(factory);
        await store.SavePersonAsync(new() { Name = new() { FirstName = "Ada", LastName = "Lovelace" } }, TestCt);
        await store.SavePersonAsync(new() { Name = new() { FirstName = "Grace", LastName = "Hopper" } }, TestCt);
        var found = await store.SearchByDisplayNameAsync("Lovelace", TestCt);
        Assert.Contains(found, person => person.Name.LastName == "Lovelace");
        Assert.DoesNotContain(found, person => person.Name.LastName == "Hopper");
    }
}