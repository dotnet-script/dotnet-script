using Dotnet.Script.Core.Interactive.LineEditing;
using Xunit;

namespace Dotnet.Script.Tests
{
    public class LineBufferTests
    {
        private static LineBuffer CreateBuffer(string text, int caret)
        {
            var buffer = new LineBuffer();
            buffer.SetText(text);
            buffer.SetCaret(caret);
            return buffer;
        }

        [Fact]
        public void ShouldMoveToPreviousWord()
        {
            var buffer = CreateBuffer("var answer = 42;", 12);
            buffer.MoveWordLeft();
            Assert.Equal(4, buffer.Caret);
        }

        [Fact]
        public void ShouldMoveToNextWord()
        {
            var buffer = CreateBuffer("var answer = 42;", 3);
            buffer.MoveWordRight();
            Assert.Equal(10, buffer.Caret);
        }

        [Fact]
        public void ShouldKillToLineStart()
        {
            var buffer = CreateBuffer("one\ntwo three", 8);
            buffer.KillToLineStart();

            Assert.Equal("one\nthree", buffer.Text);
            Assert.Equal("two ", buffer.Killed);
        }

        [Fact]
        public void ShouldKillToLineEndWithoutTouchingFollowingLines()
        {
            var buffer = CreateBuffer("one two\nthree", 4);
            buffer.KillToLineEnd();

            Assert.Equal("one \nthree", buffer.Text);
        }

        [Fact]
        public void ShouldMoveBetweenLinesKeepingColumn()
        {
            var buffer = CreateBuffer("hello\nworld", 9);

            Assert.True(buffer.MoveUp());
            Assert.Equal(3, buffer.Caret);

            Assert.True(buffer.MoveDown());
            Assert.Equal(9, buffer.Caret);
        }

        [Fact]
        public void ShouldClampColumnWhenMovingToShorterLine()
        {
            var buffer = CreateBuffer("ab\nlonger line", 12);

            Assert.True(buffer.MoveUp());
            Assert.Equal(2, buffer.Caret);
        }

        [Fact]
        public void ShouldNotMoveUpFromFirstLine()
        {
            var buffer = CreateBuffer("single", 3);
            Assert.False(buffer.MoveUp());
            Assert.Equal(3, buffer.Caret);
        }

        [Fact]
        public void ShouldTreatSurrogatePairAsSingleCharacter()
        {
            var buffer = CreateBuffer("a\U0001F600b", 3);

            Assert.True(buffer.MoveLeft());
            Assert.Equal(1, buffer.Caret);

            buffer.SetCaret(3);
            buffer.Backspace();
            Assert.Equal("ab", buffer.Text);
        }

        [Fact]
        public void ShouldReplaceSpan()
        {
            var buffer = CreateBuffer("Cons", 4);
            buffer.Replace(0, 4, "Console");

            Assert.Equal("Console", buffer.Text);
            Assert.Equal(7, buffer.Caret);
        }
    }
}
