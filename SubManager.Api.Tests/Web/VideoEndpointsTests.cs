using SubManager.Api.Web;
using Xunit;

namespace SubManager.Api.Tests.Web;

public sealed class VideoEndpointsTests
{
    [Fact]
    public void TryNormalizeDurationStatusIds_EmptyInput_ReturnsEmptySelection()
    {
        var isValid = VideoEndpoints.TryNormalizeDurationStatusIds(null, out var ids);

        Assert.True(isValid);
        Assert.Empty(ids);
    }

    [Fact]
    public void TryNormalizeDurationStatusIds_Duplicates_ReturnsDistinctSelection()
    {
        var isValid = VideoEndpoints.TryNormalizeDurationStatusIds([2, 1, 2], out var ids);

        Assert.True(isValid);
        Assert.Equal([2, 1], ids);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TryNormalizeDurationStatusIds_NonPositiveId_IsInvalid(int id)
    {
        var isValid = VideoEndpoints.TryNormalizeDurationStatusIds([id], out _);

        Assert.False(isValid);
    }

    [Fact]
    public void TryNormalizeDurationStatusIds_MoreThanOneHundredDistinctIds_IsInvalid()
    {
        var isValid = VideoEndpoints.TryNormalizeDurationStatusIds(
            Enumerable.Range(1, 101).ToArray(),
            out _);

        Assert.False(isValid);
    }

    [Fact]
    public void TryNormalizeDurationStatusIds_ExactlyOneHundredDistinctIds_IsValid()
    {
        var isValid = VideoEndpoints.TryNormalizeDurationStatusIds(
            Enumerable.Range(1, 100).ToArray(),
            out var ids);

        Assert.True(isValid);
        Assert.Equal(100, ids.Length);
    }
}
