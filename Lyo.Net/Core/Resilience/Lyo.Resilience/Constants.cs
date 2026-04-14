namespace Lyo.Resilience;

/// <summary>Consolidated constants for the Resilience library.</summary>
public static class Constants
{
    /// <summary>Constants for resilience metric names and tags.</summary>
    public static class Metrics
    {
        /// <summary>Tag key for pipeline name.</summary>
        public const string PipelineTag = "pipeline";

        /// <summary>Retry attempt counter.</summary>
        public const string Retry = "lyo.resilience.retry";

        /// <summary>Timeout event counter.</summary>
        public const string Timeout = "lyo.resilience.timeout";

        /// <summary>Circuit breaker opened counter.</summary>
        public const string CircuitBreakerOpened = "lyo.resilience.circuit_breaker.opened";

        /// <summary>Circuit breaker closed counter.</summary>
        public const string CircuitBreakerClosed = "lyo.resilience.circuit_breaker.closed";

        /// <summary>Circuit breaker half-opened counter.</summary>
        public const string CircuitBreakerHalfOpened = "lyo.resilience.circuit_breaker.half_opened";

        /// <summary>Execution duration timing.</summary>
        public const string ExecutionDuration = "lyo.resilience.execution.duration";

        /// <summary>Execution success counter.</summary>
        public const string ExecutionSuccess = "lyo.resilience.execution.success";

        /// <summary>Execution failure counter.</summary>
        public const string ExecutionFailure = "lyo.resilience.execution.failure";

        /// <summary>Execution error (exception) recording.</summary>
        public const string ExecutionError = "lyo.resilience.execution.error";
    }
}