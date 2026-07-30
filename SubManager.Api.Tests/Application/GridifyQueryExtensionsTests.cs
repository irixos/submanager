using Gridify;
using SubManager.Api.Application;
using Xunit;

namespace SubManager.Api.Tests.Application;

public sealed class GridifyQueryExtensionsTests
{
    [Theory]
    [InlineData(-1, null)]
    [InlineData(0, null)]
    [InlineData(1, 1)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    public void ClampPageSize_Boundaries_ReturnExpectedSize(int requested, int? expected)
    {
        var query = new GridifyQuery { PageSize = requested };

        var result = query.ClampPageSize();

        Assert.Same(query, result);
        Assert.Equal(expected ?? GridifyGlobalConfiguration.DefaultPageSize, query.PageSize);
    }
}
