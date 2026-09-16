namespace Lyo.Web.Automation.Core;

/// <summary>Consolidated constants for the WebAutomation contracts library.</summary>
public static class Constants
{
    /// <summary>
    /// Base metric names (logical contract). Implementations map <see cref="Metrics" /> member names to provider-specific series (for example Selenium:
    /// <c>lyo.webautomation.selenium.*</c>, Playwright: <c>lyo.webautomation.playwright.*</c>) via each browser’s metric dictionary.
    /// </summary>
    public static class Metrics
    {
        public const string StartBrowserDuration = "lyo.webautomation.browser.start.duration";

        public const string StopBrowserDuration = "lyo.webautomation.browser.stop.duration";

        public const string PollSuccess = "lyo.webautomation.poll.success";

        public const string PollFailure = "lyo.webautomation.poll.failure";

        public const string PollDuration = "lyo.webautomation.poll.duration";

        public const string TabOperationDuration = "lyo.webautomation.tab.operation.duration";

        public const string TabOperation = "lyo.webautomation.tab.operation";

        public const string FrameOperationDuration = "lyo.webautomation.frame.operation.duration";

        public const string FrameOperation = "lyo.webautomation.frame.operation";

        public const string AlertOperationDuration = "lyo.webautomation.alert.operation.duration";

        public const string AlertOperation = "lyo.webautomation.alert.operation";

        public const string KeyboardOperationDuration = "lyo.webautomation.keyboard.operation.duration";

        public const string ControlInteraction = "lyo.webautomation.control.interaction";
    }

    /// <summary>HTTP paths for the automation run API. No leading slash. The TypeScript client uses the same spellings.</summary>
    public static class Rest
    {
        /// <summary>Route group. <c>Automation</c>.</summary>
        public const string Route = "Automation";

        /// <summary>Run collection. <c>POST</c> starts a run. <c>GET {id}</c> reads one.</summary>
        public const string Runs = "Automation/Run";

        /// <summary>Stored plans. <c>GET</c> lists names. <c>POST</c> creates an empty plan.</summary>
        public const string Plans = "Automation/Plan";

        /// <summary>One stored plan. <paramref name="name" /> is a single segment.</summary>
        public static string Plan(string name) => $"{Plans}/{Uri.EscapeDataString(name)}";

        /// <summary>One stored plan by id.</summary>
        public static string PlanById(Guid id) => $"{Plans}/id/{id}";

        /// <summary>One run.</summary>
        public static string Run(Guid runId) => $"{Runs}/{runId}";

        /// <summary>Cancel a run.</summary>
        public static string Cancel(Guid runId) => $"{Runs}/{runId}/Cancel";

        /// <summary>Plain-text run transcript.</summary>
        public static string Log(Guid runId) => $"{Runs}/{runId}/Log";

        /// <summary>PNG file names captured for a run.</summary>
        public static string Snapshots(Guid runId) => $"{Runs}/{runId}/Snapshots";

        /// <summary>One viewport PNG. <paramref name="name" /> is a single file name.</summary>
        public static string Snapshot(Guid runId, string name) => $"{Runs}/{runId}/Snapshot/{name}";

        /// <summary>Viewport PNG for one finished step, read back from file storage.</summary>
        public static string StepScreenshot(Guid runId, int index) => $"{Runs}/{runId}/Steps/{index}/Screenshot";

        /// <summary>Queryable stored plans. <c>POST .../QueryProject</c> lists them.</summary>
        public const string PlanRecords = "Automation/Plans";
    }
}