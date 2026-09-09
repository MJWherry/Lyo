namespace Lyo.Job.Web.Components;

public partial class JobWorkerInstanceGrid
{
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Job";

    private string _route => $"{BaseRoute.TrimEnd('/')}/WorkerInstance";

    private LyoDataGridProjected? _dataGrid;

    private static readonly string[] _quickSearchProperties = ["WorkerType", "MachineName"];

    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("WorkerType", "Worker Type"), new("MachineName", "Machine"), new("State")];


    private async Task ViewInstance(object? item)
    {
        if (item == null)
            return;

        var id = ProjectedValueHelper.GetValue(item, "Id");
        if (id == null)
            return;

        var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobWorkerInstanceRes>>(
            $"{_route}/QueryConcrete", new() { Keys = [[id]], Amount = 1 });
        var instance = res?.Items?.FirstOrDefault();
        if (instance == null)
            return;

        var parameters = new DialogParameters<JobWorkerInstanceView> { { d => d.Instance, instance } };
        await DialogService.ShowAsync<JobWorkerInstanceView>("Worker instance", parameters, LyoDialogPresets.Medium);
    }
}
