namespace Lyo.Job.Worker.Example;

/// <summary>Fallback values used by the sample worker that handles <c>job.run.example</c> when talking to Test API.</summary>
public static class Constants
{
    public const string ExampleWorkerType = "example";
    public const string DelaySecondsParameterKey = "DelaySeconds";
    public const string DefaultApiBaseUrl = "http://localhost:5251";
}
