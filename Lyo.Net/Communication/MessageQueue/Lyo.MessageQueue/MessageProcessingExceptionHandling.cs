namespace Lyo.MessageQueue;

/// <summary>How a processing exception is treated.</summary>
public enum MessageProcessingExceptionHandling
{
    /// <summary>Swallow the exception and ack the message off the queue.</summary>
    IgnoreAndRemoveFromQueue = 0,

    /// <summary>Ack the message off the queue and rethrow so the caller can handle it.</summary>
    ThrowAndRemoveFromQueue = 1,

    /// <summary>Put the message back on the queue so it can be retried.</summary>
    RequeueOnException = 2
}