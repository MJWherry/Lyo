namespace Lyo.Web.Automation.Playwright.Core;

/// <summary>Consolidated constants for the WebAutomation.Playwright library.</summary>
public static class Constants
{
    /// <summary>Playwright-specific metric series (see <see cref="Automation.Core.Constants.Metrics" /> for logical keys).</summary>
    public static class Metrics
    {
        public const string StartBrowserDuration = "lyo.webautomation.playwright.browser.start.duration";

        public const string StopBrowserDuration = "lyo.webautomation.playwright.browser.stop.duration";

        public const string PollSuccess = "lyo.webautomation.playwright.poll.success";

        public const string PollFailure = "lyo.webautomation.playwright.poll.failure";

        public const string PollDuration = "lyo.webautomation.playwright.poll.duration";

        public const string TabOperationDuration = "lyo.webautomation.playwright.tab.operation.duration";

        public const string TabOperation = "lyo.webautomation.playwright.tab.operation";

        public const string FrameOperationDuration = "lyo.webautomation.playwright.frame.operation.duration";

        public const string FrameOperation = "lyo.webautomation.playwright.frame.operation";

        public const string AlertOperationDuration = "lyo.webautomation.playwright.alert.operation.duration";

        public const string AlertOperation = "lyo.webautomation.playwright.alert.operation";

        public const string KeyboardOperationDuration = "lyo.webautomation.playwright.keyboard.operation.duration";

        public const string ControlInteraction = "lyo.webautomation.playwright.control.interaction";
    }
}