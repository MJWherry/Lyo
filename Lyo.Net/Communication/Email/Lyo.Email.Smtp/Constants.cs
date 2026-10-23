namespace Lyo.Email.Smtp;

/// <summary>Metric names recorded by the SMTP provider while it connects.</summary>
public static class Constants
{
    /// <summary>SMTP connect and authenticate timings.</summary>
    public static class Metrics
    {
        /// <summary>Time spent in SMTP connect.</summary>
        public const string SmtpConnectDuration = "email.smtp.connect.duration";

        /// <summary>Time spent in SMTP authenticate.</summary>
        public const string SmtpAuthenticateDuration = "email.smtp.authenticate.duration";
    }
}
