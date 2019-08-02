using Dotnet.Script.DependencyModel.ProjectSystem;
using Xunit;

namespace Dotnet.Script.Tests
{
    /// <summary>
    /// Tests based on https://docs.microsoft.com/en-us/nuget/reference/package-versioning
    /// Semantically versioned packages following the Major.Minor.Revision pattern are also considered "pinned"
    /// </summary>
    public class PackageVersionTests
    {
        [Theory]
        [InlineData("1.2.3")]
        [InlineData("1.2.3.4")]
        [InlineData("[1.2]")]
        [InlineData("[1.2.3]")]
        [InlineData("[1.2.3-beta1]")]
        public void ShouldBePinned(string version)
        {
            Assert.True(new PackageVersion(version).IsPinned);
        }

        [Theory]
        [InlineData("1.0")]
        [InlineData("(1.0,)")]
        [InlineData("(,1.0]")]
        [InlineData("[1.0,2.0]")]
        [InlineData("(1.0,2.0)")]
        [InlineData("[1.0,2.0)")]
        [InlineData("(1.0)")]
        [InlineData("")]
        public void ShouldNotBePinned(string version)
        {
            Assert.False(new PackageVersion(version).IsPinned);
        }

        [Fact]
        public void ShouldBeCaseInsensitive()
        {
            Assert.Equal(new PackageVersion("1.2.3-BETA1"), new PackageVersion("1.2.3-beta1"));
        }
    }
}