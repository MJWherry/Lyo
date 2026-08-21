namespace Lyo.Job.Worker.Host;

/// <summary>Defaults for the example worker that consumes <c>job.run.example</c> against Test API.</summary>
public static class Constants
{
    public const string ExampleWorkerType = "example";
    public const string DelaySecondsParameterKey = "DelaySeconds";
    public const string DefaultApiBaseUrl = "http://localhost:5251";
}
