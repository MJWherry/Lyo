using System.Diagnostics;
using System.Text.Json;
using Lyo.Endato.Client;
using Lyo.Endato.Client.Models.Enrichment.Request;
using Lyo.Endato.Client.Models.Enrichment.Response;
using Lyo.Endato.Client.Models.Person.Request;
using Lyo.Endato.Client.Models.Person.Response;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Endato.Web.Components;

public partial class EndatoSearchWorkbench
{
    private EndatoSearchMode _searchMode = EndatoSearchMode.Person;
    private int _activeTab;
    private bool _busy;
    private string? _error;
    private long? _lastElapsedMs;

    private EndatoPersonSearchForm? _personForm;
    private EndatoEnrichmentSearchForm? _enrichmentForm;

    private PersonQuery _personQuery = new() { FirstName = "John", LastName = "Smith", DateOfBirth = "01/15/1980" };

    private EnrichmentQuery _enrichmentQuery = new() {
        FirstName = "John",
        LastName = "Smith",
        Phone = "5125550100",
        Address = new() { AddressLine1 = "123 Main St", AddressLine2 = "Austin, TX 78701" }
    };

    private PersonQueryResponse? _personResponse;
    private EnrichmentResponse? _enrichmentResponse;

    private void SetSearchMode(EndatoSearchMode mode)
    {
        if (_searchMode == mode)
            return;

        _searchMode = mode;
        _error = null;
    }

    private Task OnPersonQueryChanged(PersonQuery query)
    {
        _personQuery = query;
        return Task.CompletedTask;
    }

    private Task OnEnrichmentQueryChanged(EnrichmentQuery query)
    {
        _enrichmentQuery = query;
        return Task.CompletedTask;
    }

    private async Task RunSearchAsync()
    {
        _error = null;
        _busy = true;
        try {
            var stopwatch = Stopwatch.StartNew();
            switch (_searchMode) {
                case EndatoSearchMode.Person: {
                    var query = _personForm?.ApplyToQuery() ?? _personQuery;
                    _personQuery = query;
                    var response = await EndatoClient.Persons.QueryPersonsAsync(query).ConfigureAwait(false);
                    _personResponse = response;
                    Snackbar.Add(response.IsError ? "Endato returned IsError=true." : "Person search completed.", response.IsError ? Severity.Warning : Severity.Success);
                    break;
                }
                case EndatoSearchMode.Enrichment: {
                    var query = _enrichmentForm?.ApplyToQuery() ?? _enrichmentQuery;
                    _enrichmentQuery = query;
                    var builder = EnrichmentQueryBuilder.New().WithName(query.FirstName, query.LastName, query.MiddleName).WithDateOfBirth(query.DateOfBirth).WithAge(query.Age).WithPhone(query.Phone).WithEmail(query.Email);
                    if (query.Address != null)
                        builder.WithAddress(a => {
                            a.AddressLine1 = query.Address.AddressLine1;
                            a.AddressLine2 = query.Address.AddressLine2;
                        });

                    var response = await EndatoClient.Enrichment.QueryEnrichmentAsync(builder).ConfigureAwait(false);
                    _enrichmentResponse = response;
                    Snackbar.Add(response.IsError ? "Endato returned IsError=true." : "Enrichment completed.", response.IsError ? Severity.Warning : Severity.Success);
                    break;
                }
            }

            stopwatch.Stop();
            _lastElapsedMs = stopwatch.ElapsedMilliseconds;
        }
        catch (Exception ex) {
            _error = ex.Message;
            Snackbar.Add(_error, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }
}
