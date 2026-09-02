using Application.Common.Pagination;
using Xunit;

namespace Application.Pagination;

public class PaginatedResultTests
{
    [Fact]
    public void Constructor_AssignsValuesAndComputesTotalPages()
    {
        var items = new[] { "a", "b", "c" };

        var result = new PaginatedResult<string>(items, 1, 20, 55);

        Assert.Equal(items, result.Items);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(55, result.TotalItems);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void TotalPages_RoundsUpPartialPage()
    {
        var result = new PaginatedResult<string>(Array.Empty<string>(), 1, 20, 41);

        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void TotalPages_IsZeroWhenTotalItemsIsZero()
    {
        var result = new PaginatedResult<string>(Array.Empty<string>(), 1, 20, 0);

        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void TotalPages_HandlesExactlyDivisibleCount()
    {
        var result = new PaginatedResult<string>(Array.Empty<string>(), 1, 20, 40);

        Assert.Equal(2, result.TotalPages);
    }
}