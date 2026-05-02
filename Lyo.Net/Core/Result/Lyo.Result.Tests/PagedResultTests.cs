namespace Lyo.Result.Tests;

public class PagedResultTests
{
    [Fact]
    public void TotalPages_CalculatedCorrectly()
    {
        var paged = new PagedResult<int>([1, 2, 3], 25, 1, 10);
        Assert.Equal(3, paged.TotalPages);
    }

    [Fact]
    public void HasNextPage_TrueWhenNotOnLastPage()
    {
        var paged = new PagedResult<int>([1], 50, 1, 10);
        Assert.True(paged.HasNextPage);
    }

    [Fact]
    public void HasNextPage_FalseOnLastPage()
    {
        var paged = new PagedResult<int>([1], 10, 1, 10);
        Assert.False(paged.HasNextPage);
    }

    [Fact]
    public void HasPreviousPage_FalseOnFirstPage()
    {
        var paged = new PagedResult<int>([1], 50, 1, 10);
        Assert.False(paged.HasPreviousPage);
    }

    [Fact]
    public void HasPreviousPage_TrueOnSubsequentPage()
    {
        var paged = new PagedResult<int>([1], 50, 2, 10);
        Assert.True(paged.HasPreviousPage);
    }

    [Fact]
    public void IsFirstPage_TrueOnPageOne()
    {
        var paged = new PagedResult<int>([], 100, 1, 10);
        Assert.True(paged.IsFirstPage);
    }

    [Fact]
    public void IsLastPage_TrueWhenOnLastPage()
    {
        var paged = new PagedResult<int>([1], 20, 2, 10);
        Assert.True(paged.IsLastPage);
    }

    [Fact]
    public void IsEmpty_TrueWhenNoItems()
    {
        var paged = PagedResult<int>.Empty();
        Assert.True(paged.IsEmpty);
        Assert.Equal(0, paged.TotalCount);
        Assert.Equal(0, paged.TotalPages);
    }

    [Fact]
    public void Offset_IsCorrectForPageTwo()
    {
        var paged = new PagedResult<int>([1, 2], 20, 2, 5);
        Assert.Equal(5, paged.Offset);
    }

    [Fact]
    public void Map_ProjectsItems_PreservesPaging()
    {
        var paged = new PagedResult<int>([1, 2, 3], 30, 2, 10);
        var mapped = paged.Map(x => x.ToString());
        Assert.Equal(["1", "2", "3"], mapped.Items);
        Assert.Equal(30, mapped.TotalCount);
        Assert.Equal(2, mapped.Page);
        Assert.Equal(10, mapped.PageSize);
    }

    [Fact]
    public void SinglePage_SetsItemsAsTotalCount()
    {
        var items = new[] { 10, 20, 30 };
        var paged = PagedResult<int>.SinglePage(items);
        Assert.Equal(3, paged.TotalCount);
        Assert.Equal(1, paged.Page);
        Assert.Equal(3, paged.Items.Count);
    }

    [Fact]
    public void TotalPages_Zero_WhenPageSizeIsZero()
    {
        var paged = new PagedResult<int>([], 10, 1, 0);
        Assert.Equal(0, paged.TotalPages);
    }

    [Fact]
    public void UsedAs_ResultDataPayload()
    {
        var paged = new PagedResult<string>(["a", "b"], 100, 3, 2);
        var result = Result<PagedResult<string>>.Success(paged);
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Data!.Page);
        Assert.Equal(100, result.Data!.TotalCount);
    }
}