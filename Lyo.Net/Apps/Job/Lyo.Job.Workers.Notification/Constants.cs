namespace Lyo.Job.Workers.Notification;

/// <summary>Worker types and parameter keys for the notification workers. One shared job definition per channel serves every workflow, so the keys are part of the contract.</summary>
public static class Constants
{
    /// <summary>Content type a webhook body is sent with when the step does not name one.</summary>
    public const string DefaultContentType = "application/json";

    /// <summary>Worker type strings. These must match <c>WorkerType</c> on the job definitions the workflow steps point at.</summary>
    public static class WorkerType
    {
        public const string Webhook = "notification.webhook";
        public const string Email = "notification.email";
        public const string Sms = "notification.sms";
    }

    /// <summary>
    /// Job parameter keys each worker reads. Declaring notification inputs as parameters is what makes them templatable: the workflow step's <c>ParametersJson</c> fills them
    /// from upstream step results, so the same definition can notify about anything.
    /// </summary>
    public static class ParameterKey
    {
        public const string Url = "Url";
        public const string Body = "Body";
        public const string Headers = "Headers";
        public const string ContentType = "ContentType";
        public const string To = "To";
        public const string Cc = "Cc";
        public const string Subject = "Subject";
        public const string IsHtml = "IsHtml";
        public const string From = "From";
        public const string FromName = "FromName";
    }

    /// <summary>Result keys each worker reports, so a downstream step can condition on the send outcome.</summary>
    public static class ResultKey
    {
        public const string StatusCode = "StatusCode";
        public const string Recipient = "Recipient";
        public const string Attempts = "Attempts";
    }
}