using MyPersonalGit.Data;

namespace MyPersonalGit.Tests;

public class SelfUpdateTests
{
    [Fact]
    public void LatestVersionTag_PicksHighestSemver_IgnoringNonVersionTags()
    {
        var tags = new[] { "latest", "v1.15.3", "v1.9.0", "v1.15.10", "edge", "sha-abc123" };

        Assert.Equal("v1.15.10", SelfUpdateService.LatestVersionTag(tags));
    }

    [Fact]
    public void LatestVersionTag_ReturnsNull_WhenNoVersionTags()
    {
        Assert.Null(SelfUpdateService.LatestVersionTag(new[] { "latest", "edge" }));
    }

    [Theory]
    [InlineData("v1.15.3", "v1.15.4", true)]
    [InlineData("v1.15.3", "v2.0.0", true)]
    [InlineData("v1.15.3", "v1.15.3", false)]
    [InlineData("v1.15.4", "v1.15.3", false)]
    [InlineData("1.15.3", "v1.15.4", true)]   // tolerates missing "v" prefix
    [InlineData("dev", "v1.15.4", false)]      // dev builds never claim an update
    [InlineData("v1.15.3", "latest", false)]   // non-version candidate never wins
    public void IsNewer_ComparesVersionTags(string current, string candidate, bool expected)
    {
        Assert.Equal(expected, SelfUpdateService.IsNewer(current, candidate));
    }
}
