namespace Lyo.Web.Automation.Selenium.Core;

/// <summary>Shared constants for the WebAutomation.Selenium library.</summary>
public static class Constants
{
    /// <summary>Selenium-specific metric series (see <see cref="Web.Automation.Core.Constants.Metrics" /> for the logical keys).</summary>
    public static class Metrics
    {
        /// <summary>Duration metric for browser-start operations.</summary>
        public const string StartBrowserDuration = "lyo.webautomation.selenium.browser.start.duration";

        /// <summary>Duration metric for browser-stop operations.</summary>
        public const string StopBrowserDuration = "lyo.webautomation.selenium.browser.stop.duration";

        /// <summary>Counter metric for successful poll attempts.</summary>
        public const string PollSuccess = "lyo.webautomation.selenium.poll.success";

        /// <summary>Counter metric for failed poll attempts.</summary>
        public const string PollFailure = "lyo.webautomation.selenium.poll.failure";

        /// <summary>Duration metric for poll attempts.</summary>
        public const string PollDuration = "lyo.webautomation.selenium.poll.duration";

        /// <summary>Duration of tab and window operations (tag: operation).</summary>
        public const string TabOperationDuration = "lyo.webautomation.selenium.tab.operation.duration";

        /// <summary>Counter for tab-operation outcomes (tag: operation, result).</summary>
        public const string TabOperation = "lyo.webautomation.selenium.tab.operation";

        /// <summary>Duration of frame-context switches (tag: operation).</summary>
        public const string FrameOperationDuration = "lyo.webautomation.selenium.frame.operation.duration";

        /// <summary>Counter for frame-context operations (tag: operation, result).</summary>
        public const string FrameOperation = "lyo.webautomation.selenium.frame.operation";

        /// <summary>Duration of alert and JS dialog handling (tag: operation).</summary>
        public const string AlertOperationDuration = "lyo.webautomation.selenium.alert.operation.duration";

        /// <summary>Counter for alert-handling operations (tag: operation, result).</summary>
        public const string AlertOperation = "lyo.webautomation.selenium.alert.operation";

        /// <summary>Duration of keyboard and Actions API usage.</summary>
        public const string KeyboardOperationDuration = "lyo.webautomation.selenium.keyboard.operation.duration";

        /// <summary>Counter for control interactions (tags: control, operation).</summary>
        public const string ControlInteraction = "lyo.webautomation.selenium.control.interaction";
    }
}