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
        [InlineData("1.2.3-beta1")]
        [InlineData("0.1.4-beta")]             // See: https://github.com/filipw/dotnet-script/issues/407#issuecomment-563363947
        [InlineData("2.0.0-preview3.20122.2")] // See: https://github.com/filipw/dotnet-script/issues/407#issuecomment-631122591
        [InlineData("1.0.0-ci-20180920T1656")]
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