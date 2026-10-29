using LogistiFlow.BuildingBlocks.Application.Paging.Models;

namespace LogistiFlow.BuildingBlocks.UnitTests.Application;

public sealed class PageRequestTests
{
    [Theory]
    [InlineData(null, null, 1, 20)]
    [InlineData(0, 0, 1, 1)]
    [InlineData(-5, 500, 1, 100)]
    [InlineData(3, 10, 3, 10)]
    public void Normalize_ClampsToAValidWindow(int? page, int? pageSize, int expectedPage, int expectedSize)
    {
        var normalized = PageRequest.Normalize(page, pageSize);

        normalized.Page.Should().Be(expectedPage);
        normalized.PageSize.Should().Be(expectedSize);
    }

    [Fact]
    public void Offset_SkipsPreviousPages()
    {
        new PageRequest(3, 25).Offset.Should().Be(50);
    }

    [Fact]
    public void TotalPages_RoundsUp()
    {
        new PagedResult<int>([], 1, 20, 41).TotalPages.Should().Be(3);
    }
}
