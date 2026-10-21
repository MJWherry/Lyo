namespace Lyo.Outbox.Dispatcher;

/// <summary>Poll, lease, and retry settings for <see cref="OutboxDispatcher" />.</summary>
public sealed class OutboxDispatcherOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "OutboxDispatcher";

    /// <summary>Delay between polls.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Maximum rows claimed in one poll.</summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>How long a claimed row is hidden from other workers. Default 30 seconds.</summary>
    public TimeSpan Lease { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Failed publishes that reach this count are marked failed.</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Delay before a failed publish can be claimed again.</summary>
    public TimeSpan Backoff { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Throws when a limit is not usable.</summary>
    public void Validate()
    {
        if (PollInterval <= TimeSpan.Zero)
            throw new ArgumentException("PollInterval must be positive.", nameof(PollInterval));

        if (BatchSize < 1)
            throw new ArgumentException("BatchSize must be at least 1.", nameof(BatchSize));

        if (Lease <= TimeSpan.Zero)
            throw new ArgumentException("Lease must be positive.", nameof(Lease));

        if (MaxAttempts < 1)
            throw new ArgumentException("MaxAttempts must be at least 1.", nameof(MaxAttempts));

        if (Backoff < TimeSpan.Zero)
            throw new ArgumentException("Backoff cannot be negative.", nameof(Backoff));
    }
}