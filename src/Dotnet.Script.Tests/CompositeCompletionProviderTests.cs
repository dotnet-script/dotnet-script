using Dotnet.Script.Core.Interactive.LineEditing;
using Xunit;

namespace Dotnet.Script.Tests
{
    public class CompositeCompletionProviderTests
    {
        private sealed class StubCompletionProvider : ICompletionProvider
        {
            private readonly CompletionResult _result;

            public StubCompletionProvider(CompletionResult result)
            {
                _result = result;
            }

            public bool WasCalled { get; private set; }

            public CompletionResult GetCompletions(string text, int caret)
            {
                WasCalled = true;
                return _result;
            }
        }

        private static CompletionResult Result(params string[] items) => new CompletionResult(0, 0, items);

        [Fact]
        public void ShouldPreferThePrimaryProvider()
        {
            var provider = new CompositeCompletionProvider(new StubCompletionProvider(Result("FromRoslyn")), new ReplCompletionProvider());

            Assert.Equal(new[] { "FromRoslyn" }, provider.GetCompletions("fore", 4).Items);
        }

        [Fact]
        public void ShouldNotFallBackWhenThePrimaryAnswersWithNoResults()
        {
            var provider = new CompositeCompletionProvider(new StubCompletionProvider(CompletionResult.Empty), new ReplCompletionProvider());

            Assert.True(provider.GetCompletions("fore", 4).IsEmpty);
        }

        [Fact]
        public void ShouldFallBackWhenThePrimaryCannotAnswer()
        {
            var provider = new CompositeCompletionProvider(new StubCompletionProvider(null), new ReplCompletionProvider());

            Assert.Contains("foreach", provider.GetCompletions("fore", 4).Items);
        }

        [Fact]
        public void ShouldFallBackWhenThereIsNoPrimary()
        {
            var provider = new CompositeCompletionProvider(null, new ReplCompletionProvider());

            Assert.Contains("foreach", provider.GetCompletions("fore", 4).Items);
        }

        [Fact]
        public void ShouldNotAskThePrimaryAboutDirectives()
        {
            var primary = new StubCompletionProvider(Result("FromRoslyn"));
            var provider = new CompositeCompletionProvider(primary, new ReplCompletionProvider());

            Assert.Equal(new[] { "#load" }, provider.GetCompletions("#lo", 3).Items);
            Assert.False(primary.WasCalled);
        }
    }
}
