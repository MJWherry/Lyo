namespace Lyo.Resilience;

/// <summary>Shared constants for the Resilience library.</summary>
public static class Constants
{
    /// <summary>Resilience metric names and tag keys.</summary>
    public static class Metrics
    {
        /// <summary>Tag key whose value is the pipeline name.</summary>
        public const string PipelineTag = "pipeline";

        /// <summary>Counter: retry attempts.</summary>
        public const string Retry = "lyo.resilience.retry";

        /// <summary>Counter: timeout events.</summary>
        public const string Timeout = "lyo.resilience.timeout";

        /// <summary>Counter: circuit breaker opened.</summary>
        public const string CircuitBreakerOpened = "lyo.resilience.circuit_breaker.opened";

        /// <summary>Counter: circuit breaker closed.</summary>
        public const string CircuitBreakerClosed = "lyo.resilience.circuit_breaker.closed";

        /// <summary>Counter: circuit breaker half-opened.</summary>
        public const string CircuitBreakerHalfOpened = "lyo.resilience.circuit_breaker.half_opened";

        /// <summary>Timer: execution duration.</summary>
        public const string ExecutionDuration = "lyo.resilience.execution.duration";

        /// <summary>Counter: execution success.</summary>
        public const string ExecutionSuccess = "lyo.resilience.execution.success";

        /// <summary>Counter: execution failure.</summary>
        public const string ExecutionFailure = "lyo.resilience.execution.failure";

        /// <summary>Error recording: unhandled exception during execution.</summary>
        public const string ExecutionError = "lyo.resilience.execution.error";
    }
}