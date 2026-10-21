namespace Lyo.Web.Automation.PuppeteerSharp.Core;

/// <summary>Shared constants for the WebAutomation.PuppeteerSharp library.</summary>
public static class Constants
{
    /// <summary>PuppeteerSharp metric series.</summary>
    public static class Metrics
    {
        public const string StartBrowserDuration = "lyo.webautomation.puppeteersharp.browser.start.duration";

        public const string StopBrowserDuration = "lyo.webautomation.puppeteersharp.browser.stop.duration";

        public const string PollSuccess = "lyo.webautomation.puppeteersharp.poll.success";

        public const string PollFailure = "lyo.webautomation.puppeteersharp.poll.failure";

        public const string PollDuration = "lyo.webautomation.puppeteersharp.poll.duration";

        public const string TabOperationDuration = "lyo.webautomation.puppeteersharp.tab.operation.duration";

        public const string TabOperation = "lyo.webautomation.puppeteersharp.tab.operation";

        public const string FrameOperationDuration = "lyo.webautomation.puppeteersharp.frame.operation.duration";

        public const string FrameOperation = "lyo.webautomation.puppeteersharp.frame.operation";

        public const string AlertOperationDuration = "lyo.webautomation.puppeteersharp.alert.operation.duration";

        public const string AlertOperation = "lyo.webautomation.puppeteersharp.alert.operation";

        public const string KeyboardOperationDuration = "lyo.webautomation.puppeteersharp.keyboard.operation.duration";

        public const string ControlInteraction = "lyo.webautomation.puppeteersharp.control.interaction";
    }
}