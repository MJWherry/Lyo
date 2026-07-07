using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Sms.Models;

namespace Lyo.Sms.Twilio.Builders;

/// <summary>Fluent builder for constructing <see cref="SmsMessageQueryFilter" /> instances for querying Twilio messages, with validation and phone number normalization.</summary>
/// <remarks>
/// <para>
/// Twilio's list API does not support server-side direction filtering, so direction criteria set via <see cref="WithDirection" />, <see cref="Inbound" />, or
/// <see cref="Outbound" /> are applied client-side by <see cref="TwilioSmsService.GetMessagesAsync(SmsMessageQueryFilter, CancellationToken)" />.
/// </para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class TwilioMessageQueryBuilder
{
    /// <summary>Maximum page size supported by the Twilio API.</summary>
    public const int MaxPageSize = 1000;

    private readonly SmsMessageQueryFilter _filter = new();

    /// <summary>Filters by sender phone number.</summary>
    /// <param name="from">The sender phone number in E.164 format or US format. Normalized to E.164.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when the phone number is null or empty.</exception>
    /// <exception cref="InvalidFormatException">Thrown when the phone number format is invalid.</exception>
    public TwilioMessageQueryBuilder WithFrom(string from)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(from);
        if (!PhoneNumber.IsValid(from))
            throw new InvalidFormatException("From phone number is not in a valid format.", nameof(from), from, PhoneNumber.ValidFormats);

        _filter.From = PhoneNumber.Normalize(from);
        return this;
    }

    /// <summary>Filters by recipient phone number.</summary>
    /// <param name="to">The recipient phone number in E.164 format or US format. Normalized to E.164.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when the phone number is null or empty.</exception>
    /// <exception cref="InvalidFormatException">Thrown when the phone number format is invalid.</exception>
    public TwilioMessageQueryBuilder WithTo(string to)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(to);
        if (!PhoneNumber.IsValid(to))
            throw new InvalidFormatException("To phone number is not in a valid format.", nameof(to), to, PhoneNumber.ValidFormats);

        _filter.To = PhoneNumber.Normalize(to);
        return this;
    }

    /// <summary>Filters messages sent on or after the specified date.</summary>
    /// <param name="dateSentAfter">The lower date bound. Local dates are converted to UTC by the service.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public TwilioMessageQueryBuilder WithDateSentAfter(DateTime dateSentAfter)
    {
        _filter.DateSentAfter = dateSentAfter;
        return this;
    }

    /// <summary>Filters messages sent on or before the specified date.</summary>
    /// <param name="dateSentBefore">The upper date bound. Local dates are converted to UTC by the service.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public TwilioMessageQueryBuilder WithDateSentBefore(DateTime dateSentBefore)
    {
        _filter.DateSentBefore = dateSentBefore;
        return this;
    }

    /// <summary>Filters messages sent within the specified date range.</summary>
    /// <param name="after">The lower date bound (inclusive).</param>
    /// <param name="before">The upper date bound (inclusive).</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when <paramref name="after" /> is later than <paramref name="before" />.</exception>
    public TwilioMessageQueryBuilder WithDateRange(DateTime after, DateTime before)
    {
        if (after > before)
            throw new ArgumentOutsideRangeException(nameof(after), after, DateTime.MinValue, before, "DateSentAfter must be earlier than or equal to DateSentBefore.");

        _filter.DateSentAfter = after;
        _filter.DateSentBefore = before;
        return this;
    }

    /// <summary>Sets the number of messages per page.</summary>
    /// <param name="pageSize">The page size (1–1000).</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when the page size is outside the 1–1000 range.</exception>
    public TwilioMessageQueryBuilder WithPageSize(int pageSize)
    {
        ArgumentHelpers.ThrowIfNotInRange(pageSize, 1, MaxPageSize);
        _filter.PageSize = pageSize;
        return this;
    }

    /// <summary>Adds a message direction to filter by. Multiple directions are combined with OR semantics.</summary>
    /// <param name="direction">The message direction to include.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public TwilioMessageQueryBuilder WithDirection(Direction direction)
    {
        if (!_filter.Directions.Contains(direction))
            _filter.Directions.Add(direction);

        return this;
    }

    /// <summary>Filters to inbound messages only (messages received by your Twilio numbers).</summary>
    /// <returns>The builder instance for method chaining.</returns>
    public TwilioMessageQueryBuilder Inbound() => WithDirection(Direction.Inbound);

    /// <summary>Filters to outbound messages (sent via API, call, or reply).</summary>
    /// <returns>The builder instance for method chaining.</returns>
    public TwilioMessageQueryBuilder Outbound() => WithDirection(Direction.OutboundApi).WithDirection(Direction.OutboundCall).WithDirection(Direction.OutboundReply);

    /// <summary>Sets the cursor for the next page using the <c>NextCursor</c> value from a previous <see cref="SmsMessageQueryResults{T}" />.</summary>
    /// <param name="cursor">The cursor (oldest DateSent from the previous page), used as the upper date bound for the next page.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public TwilioMessageQueryBuilder WithNextPage(DateTime cursor)
    {
        _filter.DateSentBefore = cursor;
        return this;
    }

    /// <summary>Clears all filter criteria and restores defaults.</summary>
    /// <returns>The builder instance for method chaining.</returns>
    public TwilioMessageQueryBuilder Clear()
    {
        _filter.From = null;
        _filter.To = null;
        _filter.DateSentAfter = null;
        _filter.DateSentBefore = null;
        _filter.PageSize = new SmsMessageQueryFilter().PageSize;
        _filter.Directions.Clear();
        return this;
    }

    /// <summary>Builds and validates the message query filter.</summary>
    /// <returns>A validated <see cref="SmsMessageQueryFilter" /> instance.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when DateSentAfter is later than DateSentBefore.</exception>
    public SmsMessageQueryFilter Build()
    {
        if (_filter.DateSentAfter.HasValue && _filter.DateSentBefore.HasValue && _filter.DateSentAfter.Value > _filter.DateSentBefore.Value) {
            throw new ArgumentOutsideRangeException(
                nameof(_filter.DateSentAfter), _filter.DateSentAfter.Value, DateTime.MinValue, _filter.DateSentBefore.Value,
                "DateSentAfter must be earlier than or equal to DateSentBefore.");
        }

        var filter = new SmsMessageQueryFilter {
            From = _filter.From,
            To = _filter.To,
            DateSentAfter = _filter.DateSentAfter,
            DateSentBefore = _filter.DateSentBefore,
            PageSize = _filter.PageSize
        };

        foreach (var direction in _filter.Directions)
            filter.Directions.Add(direction);

        return filter;
    }

    /// <summary>Creates a new instance of TwilioMessageQueryBuilder.</summary>
    /// <returns>A new TwilioMessageQueryBuilder instance.</returns>
    public static TwilioMessageQueryBuilder New() => new();

    /// <summary>Returns a diagnostic string for the current query builder state.</summary>
    /// <returns>A string containing the current filter criteria.</returns>
    public override string ToString() => _filter.ToString();
}
