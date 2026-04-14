using Lyo.Endato.Client.Models.Enrichment.Request;
using Lyo.Endato.Client.Models.Enrichment.Response;

namespace Lyo.Endato.Client;

public class EnrichmentManager(EndatoClient client)
{
    public async Task<EnrichmentResponse> QueryEnrichmentAsync(EnrichmentQuery query, CancellationToken ct = default)
    {
        //.AddHeader("galaxy-search-type", "DevAPIContactEnrich")
        var response = await client.PostAsAsync<EnrichmentQuery, EnrichmentResponse>(
                "/Contact/Enrich", query, request => request.Headers.Add("galaxy-search-type", "DevAPIContactEnrich"), ct)
            .ConfigureAwait(false);

        return response;
    }
}