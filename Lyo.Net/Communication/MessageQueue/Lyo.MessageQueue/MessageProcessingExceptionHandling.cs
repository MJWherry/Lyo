namespace Lyo.MessageQueue;

/// <summary>Defines how exceptions during message processing should be handled.</summary>
public enum MessageProcessingExceptionHandling
{
    /// <summary>Ignore the exception and remove the message from the queue (acknowledge it).</summary>
    IgnoreAndRemoveFromQueue = 0,

    /// <summary>Throw the exception and remove the message from the queue (acknowledge it). This allows the exception to propagate to the caller for handling.</summary>
    ThrowAndRemoveFromQueue = 1,

    /// <summary>Requeue the message when an exception occurs, allowing it to be retried.</summary>
    RequeueOnException = 2
}