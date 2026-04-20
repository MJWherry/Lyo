namespace Lyo.Web.Automation;

/// <summary>Consolidated constants for the WebAutomation contracts library.</summary>
public static class Constants
{
    /// <summary>
    /// Base metric names (logical contract). Implementations map <see cref="Metrics" /> member names to
    /// provider-specific series (e.g. Selenium: <c>lyo.webautomation.selenium.*</c>, Playwright: <c>lyo.webautomation.playwright.*</c>) via each browser’s metric dictionary.
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
}
