using Lyo.Exceptions.Models;
using Lyo.Sms.Models;
using Lyo.Sms.Twilio.Builders;

namespace Lyo.Sms.Tests;

public class TwilioMessageQueryBuilderTests
{
    [Fact]
    public void New_ReturnsNewInstance()
    {
        var builder = TwilioMessageQueryBuilder.New();
        Assert.NotNull(builder);
        Assert.NotSame(builder, TwilioMessageQueryBuilder.New());
    }

    [Fact]
    public void WithFrom_ValidPhoneNumber_ReturnsBuilder()
    {
        var builder = TwilioMessageQueryBuilder.New();
        var result = builder.WithFrom("+15551234567");
        Assert.Same(builder, result);
    }

    [Fact]
    public void WithFrom_USFormat_NormalizesToE164()
    {
        var filter = TwilioMessageQueryBuilder.New().WithFrom("555-123-4567").Build();
        Assert.Equal("+15551234567", filter.From);
    }

    [Fact]
    public void WithFrom_Null_ThrowsArgumentNullException()
    {
        var builder = TwilioMessageQueryBuilder.New();
        Assert.Throws<ArgumentNullException>(() => builder.WithFrom(null!));
    }

    [Fact]
    public void WithFrom_Empty_ThrowsArgumentException()
    {
        var builder = TwilioMessageQueryBuilder.New();
        Assert.Throws<ArgumentException>(() => builder.WithFrom(""));
    }

    [Fact]
    public void WithFrom_InvalidFormat_ThrowsInvalidFormatException()
    {
        var builder = TwilioMessageQueryBuilder.New();
        var exception = Assert.Throws<InvalidFormatException>(() => builder.WithFrom("invalid"));
        Assert.Equal("invalid", exception.InvalidValue);
        Assert.True(exception.ValidFormats.Count > 0);
    }

    [Fact]
    public void WithTo_ValidPhoneNumber_ReturnsBuilder()
    {
        var builder = TwilioMessageQueryBuilder.New();
        var result = builder.WithTo("+15551234567");
        Assert.Same(builder, result);
    }

    [Fact]
    public void WithTo_USFormat_NormalizesToE164()
    {
        var filter = TwilioMessageQueryBuilder.New().WithTo("(555) 123-4567").Build();
        Assert.Equal("+15551234567", filter.To);
    }

    [Fact]
    public void WithTo_InvalidFormat_ThrowsInvalidFormatException()
    {
        var builder = TwilioMessageQueryBuilder.New();
        Assert.Throws<InvalidFormatException>(() => builder.WithTo("not-a-number"));
    }

    [Fact]
    public void WithDateSentAfter_SetsFilterValue()
    {
        var after = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var filter = TwilioMessageQueryBuilder.New().WithDateSentAfter(after).Build();
        Assert.Equal(after, filter.DateSentAfter);
    }

    [Fact]
    public void WithDateSentBefore_SetsFilterValue()
    {
        var before = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var filter = TwilioMessageQueryBuilder.New().WithDateSentBefore(before).Build();
        Assert.Equal(before, filter.DateSentBefore);
    }

    [Fact]
    public void WithDateRange_ValidRange_SetsBothBounds()
    {
        var after = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var before = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var filter = TwilioMessageQueryBuilder.New().WithDateRange(after, before).Build();
        Assert.Equal(after, filter.DateSentAfter);
        Assert.Equal(before, filter.DateSentBefore);
    }

    [Fact]
    public void WithDateRange_AfterLaterThanBefore_ThrowsArgumentOutsideRangeException()
    {
        var after = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var before = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var builder = TwilioMessageQueryBuilder.New();
        Assert.Throws<ArgumentOutsideRangeException>(() => builder.WithDateRange(after, before));
    }

    [Fact]
    public void Build_InvertedDateBounds_ThrowsArgumentOutsideRangeException()
    {
        var builder = TwilioMessageQueryBuilder.New().WithDateSentAfter(new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)).WithDateSentBefore(new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.Throws<ArgumentOutsideRangeException>(builder.Build);
    }

    [Fact]
    public void WithPageSize_ValidValue_SetsFilterValue()
    {
        var filter = TwilioMessageQueryBuilder.New().WithPageSize(100).Build();
        Assert.Equal(100, filter.PageSize);
    }

    [Fact]
    public void WithPageSize_Zero_ThrowsArgumentOutOfRangeException()
    {
        var builder = TwilioMessageQueryBuilder.New();
        Assert.Throws<ArgumentOutsideRangeException>(() => builder.WithPageSize(0));
    }

    [Fact]
    public void WithPageSize_ExceedsMax_ThrowsArgumentOutOfRangeException()
    {
        var builder = TwilioMessageQueryBuilder.New();
        Assert.Throws<ArgumentOutsideRangeException>(() => builder.WithPageSize(TwilioMessageQueryBuilder.MaxPageSize + 1));
    }

    [Fact]
    public void WithPageSize_MaxValue_Works()
    {
        var filter = TwilioMessageQueryBuilder.New().WithPageSize(TwilioMessageQueryBuilder.MaxPageSize).Build();
        Assert.Equal(TwilioMessageQueryBuilder.MaxPageSize, filter.PageSize);
    }

    [Fact]
    public void Build_DefaultPageSize_Is50()
    {
        var filter = TwilioMessageQueryBuilder.New().Build();
        Assert.Equal(50, filter.PageSize);
    }

    [Fact]
    public void WithDirection_AddsDirection()
    {
        var filter = TwilioMessageQueryBuilder.New().WithDirection(Direction.OutboundApi).Build();
        Assert.Single(filter.Directions);
        Assert.Contains(Direction.OutboundApi, filter.Directions);
    }

    [Fact]
    public void WithDirection_Duplicate_AddsOnce()
    {
        var filter = TwilioMessageQueryBuilder.New().WithDirection(Direction.Inbound).WithDirection(Direction.Inbound).Build();
        Assert.Single(filter.Directions);
    }

    [Fact]
    public void Inbound_AddsInboundDirection()
    {
        var filter = TwilioMessageQueryBuilder.New().Inbound().Build();
        Assert.Single(filter.Directions);
        Assert.Contains(Direction.Inbound, filter.Directions);
    }

    [Fact]
    public void Outbound_AddsAllOutboundDirections()
    {
        var filter = TwilioMessageQueryBuilder.New().Outbound().Build();
        Assert.Equal(3, filter.Directions.Count);
        Assert.Contains(Direction.OutboundApi, filter.Directions);
        Assert.Contains(Direction.OutboundCall, filter.Directions);
        Assert.Contains(Direction.OutboundReply, filter.Directions);
    }

    [Fact]
    public void InboundAndOutbound_Combined_AddsAllDirections()
    {
        var filter = TwilioMessageQueryBuilder.New().Inbound().Outbound().Build();
        Assert.Equal(4, filter.Directions.Count);
    }

    [Fact]
    public void Build_NoDirections_LeavesDirectionsEmpty()
    {
        var filter = TwilioMessageQueryBuilder.New().WithTo("+15551234567").Build();
        Assert.Empty(filter.Directions);
    }

    [Fact]
    public void WithNextPage_SetsDateSentBefore()
    {
        var cursor = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        var filter = TwilioMessageQueryBuilder.New().WithNextPage(cursor).Build();
        Assert.Equal(cursor, filter.DateSentBefore);
    }

    [Fact]
    public void Clear_ResetsAllCriteria()
    {
        var builder = TwilioMessageQueryBuilder.New()
            .WithFrom("+15551234567")
            .WithTo("+19876543210")
            .WithDateRange(new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc))
            .WithPageSize(200)
            .Inbound();

        var filter = builder.Clear().Build();
        Assert.Null(filter.From);
        Assert.Null(filter.To);
        Assert.Null(filter.DateSentAfter);
        Assert.Null(filter.DateSentBefore);
        Assert.Equal(50, filter.PageSize);
        Assert.Empty(filter.Directions);
    }

    [Fact]
    public void Build_ReturnsIndependentFilters()
    {
        var builder = TwilioMessageQueryBuilder.New().Inbound();
        var first = builder.Build();
        var second = builder.Build();
        Assert.NotSame(first, second);
        first.Directions.Clear();
        Assert.Single(second.Directions);
    }

    [Fact]
    public void Build_FullChain_ProducesExpectedFilter()
    {
        var after = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var before = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var filter = TwilioMessageQueryBuilder.New().WithFrom("+15551234567").WithTo("+19876543210").WithDateRange(after, before).WithPageSize(25).Outbound().Build();
        Assert.Equal("+15551234567", filter.From);
        Assert.Equal("+19876543210", filter.To);
        Assert.Equal(after, filter.DateSentAfter);
        Assert.Equal(before, filter.DateSentBefore);
        Assert.Equal(25, filter.PageSize);
        Assert.Equal(3, filter.Directions.Count);
    }

    [Fact]
    public void ToString_IncludesCriteria()
    {
        var text = TwilioMessageQueryBuilder.New().WithFrom("+15551234567").Inbound().ToString();
        Assert.Contains("+15551234567", text);
        Assert.Contains("Inbound", text);
    }

    [Fact]
    public void ToString_NoDirections_ShowsAny()
    {
        var text = TwilioMessageQueryBuilder.New().ToString();
        Assert.Contains("(any)", text);
    }
}