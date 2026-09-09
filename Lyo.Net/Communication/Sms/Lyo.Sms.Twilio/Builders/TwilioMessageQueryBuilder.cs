using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Sms.Models;

namespace Lyo.Sms.Twilio.Builders;

/// <summary>Fluent helper that builds a validated <see cref="SmsMessageQueryFilter" /> for Twilio message queries, including number normalization.</summary>
/// <remarks>
/// <para>
/// Twilio's list API cannot filter by direction on the server, so criteria from <see cref="WithDirection" />, <see cref="Inbound" />, or
/// <see cref="Outbound" /> are applied on the client in <see cref="TwilioSmsService.GetMessagesAsync(SmsMessageQueryFilter, CancellationToken)" />.
/// </para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class TwilioMessageQueryBuilder
{
    /// <summary>Largest page size the Twilio API accepts.</summary>
    public const int MaxPageSize = 1000;

    private readonly SmsMessageQueryFilter _filter = new();

    /// <summary>Restricts results to a sender number.</summary>
    /// <param name="from">Sender in E.164 or US format; stored as E.164.</param>
    /// <returns>This builder so further calls can chain.</returns>
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

    /// <summary>Restricts results to a recipient number.</summary>
    /// <param name="to">Recipient in E.164 or US format; stored as E.164.</param>
    /// <returns>This builder so further calls can chain.</returns>
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

    /// <summary>Keeps messages sent on or after this date.</summary>
    /// <param name="dateSentAfter">Lower bound. The service converts local dates to UTC.</param>
    /// <returns>This builder so further calls can chain.</returns>
    public TwilioMessageQueryBuilder WithDateSentAfter(DateTime dateSentAfter)
    {
        _filter.DateSentAfter = dateSentAfter;
        return this;
    }

    /// <summary>Keeps messages sent on or before this date.</summary>
    /// <param name="dateSentBefore">Upper bound. The service converts local dates to UTC.</param>
    /// <returns>This builder so further calls can chain.</returns>
    public TwilioMessageQueryBuilder WithDateSentBefore(DateTime dateSentBefore)
    {
        _filter.DateSentBefore = dateSentBefore;
        return this;
    }

    /// <summary>Keeps messages sent in an inclusive date window.</summary>
    /// <param name="after">Inclusive lower bound.</param>
    /// <param name="before">Inclusive upper bound.</param>
    /// <returns>This builder so further calls can chain.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when <paramref name="after" /> is later than <paramref name="before" />.</exception>
    public TwilioMessageQueryBuilder WithDateRange(DateTime after, DateTime before)
    {
        if (after > before)
            throw new ArgumentOutsideRangeException(nameof(after), after, DateTime.MinValue, before, "DateSentAfter must be earlier than or equal to DateSentBefore.");

        _filter.DateSentAfter = after;
        _filter.DateSentBefore = before;
        return this;
    }

    /// <summary>How many messages each page should contain.</summary>
    /// <param name="pageSize">Page size in the 1–1000 range.</param>
    /// <returns>This builder so further calls can chain.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when the page size is outside the 1–1000 range.</exception>
    public TwilioMessageQueryBuilder WithPageSize(int pageSize)
    {
        ArgumentHelpers.ThrowIfNotInRange(pageSize, 1, MaxPageSize);
        _filter.PageSize = pageSize;
        return this;
    }

    /// <summary>Includes a direction in the filter. Several calls are combined with OR.</summary>
    /// <param name="direction">Direction to include.</param>
    /// <returns>This builder so further calls can chain.</returns>
    public TwilioMessageQueryBuilder WithDirection(Direction direction)
    {
        if (!_filter.Directions.Contains(direction))
            _filter.Directions.Add(direction);

        return this;
    }

    /// <summary>Restricts results to inbound traffic (received on your Twilio numbers).</summary>
    /// <returns>This builder so further calls can chain.</returns>
    public TwilioMessageQueryBuilder Inbound() => WithDirection(Direction.Inbound);

    /// <summary>Restricts results to outbound traffic (API, call, or reply).</summary>
    /// <returns>This builder so further calls can chain.</returns>
    public TwilioMessageQueryBuilder Outbound() => WithDirection(Direction.OutboundApi).WithDirection(Direction.OutboundCall).WithDirection(Direction.OutboundReply);

    /// <summary>Advances to the next page using <c>NextCursor</c> from a prior <see cref="SmsMessageQueryResults{T}" />.</summary>
    /// <param name="cursor">Cursor (oldest DateSent on the last page); becomes the upper date bound for the next page.</param>
    /// <returns>This builder so further calls can chain.</returns>
    public TwilioMessageQueryBuilder WithNextPage(DateTime cursor)
    {
        _filter.DateSentBefore = cursor;
        return this;
    }

    /// <summary>Drops every filter and restores defaults.</summary>
    /// <returns>This builder so further calls can chain.</returns>
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

    /// <summary>Validates criteria and produces an <see cref="SmsMessageQueryFilter" />.</summary>
    /// <returns>A validated <see cref="SmsMessageQueryFilter" />.</returns>
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

    /// <summary>Starts a new TwilioMessageQueryBuilder.</summary>
    /// <returns>A fresh TwilioMessageQueryBuilder.</returns>
    public static TwilioMessageQueryBuilder New() => new();

    /// <summary>Diagnostic snapshot of the current query builder.</summary>
    /// <returns>Text describing the active filter criteria.</returns>
    public override string ToString() => _filter.ToString();
}