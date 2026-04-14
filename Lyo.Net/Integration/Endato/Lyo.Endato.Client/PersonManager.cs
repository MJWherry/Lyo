using Lyo.Endato.Client.Models.Person.Request;
using Lyo.Endato.Client.Models.Person.Response;

namespace Lyo.Endato.Client;

public class PersonManager(EndatoClient client)
{
    public async Task<PersonQueryResponse> QueryPersonsAsync(PersonQuery query, CancellationToken ct = default)
    {
        var response = await client.PostAsAsync<PersonQuery, PersonQueryResponse>("/PersonSearch", query, request => request.Headers.Add("galaxy-search-type", "Person"), ct)
            .ConfigureAwait(false);

        return response;
    }
}