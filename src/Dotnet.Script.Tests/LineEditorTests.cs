using System;
using Dotnet.Script.Core.Interactive.LineEditing;
using Xunit;
using Key = Dotnet.Script.Tests.FakeConsoleDevice.Key;

namespace Dotnet.Script.Tests
{
    public class LineEditorTests
    {
        private static LineEditor CreateEditor(FakeConsoleDevice device) =>
            new LineEditor(device, ReplColorScheme.Default, new ReplHistory());

        private static string Read(LineEditor editor, Func<string, bool> isComplete = null) =>
            editor.Read("> ", "* ", isComplete);

        [Fact]
        public void ShouldReturnTypedInput()
        {
            var device = new FakeConsoleDevice();
            device.Enqueue(Key.Type("1+1"));
            device.Enqueue(Key.Enter);

            Assert.Equal("1+1", Read(CreateEditor(device)));
        }

        [Fact]
        public void ShouldRenderPromptAndInput()
        {
            var device = new FakeConsoleDevice();
            device.Enqueue(Key.Type("1+1"));
            device.Enqueue(Key.Enter);

            Read(CreateEditor(device));

            Assert.Equal("> 1+1", device.GetRow(0));
        }

        [Fact]
        public void ShouldEditWithBackspace()
        {
            var device = new FakeConsoleDevice();
            device.Enqueue(Key.Type("1+2"));
            device.Enqueue(Key.Press(ConsoleKey.Backspace));
            device.Enqueue(Key.Type("3"));
            device.Enqueue(Key.Enter);

            Assert.Equal("1+3", Read(CreateEditor(device)));
        }

        [Fact]
        public void ShouldInsertAtCaretAfterMovingLeft()
        {
            var device = new FakeConsoleDevice();
            device.Enqueue(Key.Type("13"));
            device.Enqueue(Key.Press(ConsoleKey.LeftArrow));
            device.Enqueue(Key.Type("2"));
            device.Enqueue(Key.Enter);

            Assert.Equal("123", Read(CreateEditor(device)));
        }

        [Fact]
        public void ShouldRecallPreviousSubmissionWithArrowUp()
        {
            var device = new FakeConsoleDevice();
            var editor = CreateEditor(device);

            device.Enqueue(Key.Type("var x = 1;"));
            device.Enqueue(Key.Enter);
            Read(editor);

            device.Enqueue(Key.Press(ConsoleKey.UpArrow));
            device.Enqueue(Key.Enter);

            Assert.Equal("var x = 1;", Read(editor));
        }

        [Fact]
        public void ShouldFilterHistoryByTypedPrefix()
        {
            var device = new FakeConsoleDevice();
            var editor = CreateEditor(device);

            device.Enqueue(Key.Type("foo1"));
            device.Enqueue(Key.Enter);
            Read(editor);

            device.Enqueue(Key.Type("bar"));
            device.Enqueue(Key.Enter);
            Read(editor);

            device.Enqueue(Key.Type("f"));
            device.Enqueue(Key.Press(ConsoleKey.UpArrow));
            device.Enqueue(Key.Enter);

            Assert.Equal("foo1", Read(editor));
        }

        [Fact]
        public void ShouldReturnToPendingInputWhenNavigatingForward()
        {
            var device = new FakeConsoleDevice();
            var editor = CreateEditor(device);

            device.Enqueue(Key.Type("old"));
            device.Enqueue(Key.Enter);
            Read(editor);

            device.Enqueue(Key.Type("new"));
            device.Enqueue(Key.Press(ConsoleKey.UpArrow));
            device.Enqueue(Key.Press(ConsoleKey.DownArrow));
            device.Enqueue(Key.Enter);

            Assert.Equal("new", Read(editor));
        }

        [Fact]
        public void ShouldContinueIncompleteSubmissionOnNewLine()
        {
            var device = new FakeConsoleDevice();
            device.Enqueue(Key.Type("if (true) {"));
            device.Enqueue(Key.Enter);
            device.Enqueue(Key.Type("}"));
            device.Enqueue(Key.Enter);

            var result = Read(CreateEditor(device), BraceCompleteness.IsComplete);

            Assert.Equal("if (true) {\n}", result);
            Assert.Equal("> if (true) {", device.GetRow(0));
            Assert.Equal("* }", device.GetRow(1));
        }

        [Fact]
        public void ShouldIndentInsideBlockAndDedentOnClosingBrace()
        {
            var device = new FakeConsoleDevice();
            device.Enqueue(Key.Type("{"));
            device.Enqueue(Key.Enter);
            device.Enqueue(Key.Type("1"));
            device.Enqueue(Key.Enter);
            device.Enqueue(Key.Type("}"));
            device.Enqueue(Key.Enter);

            var result = Read(CreateEditor(device), BraceCompleteness.IsComplete);

            Assert.Equal("{\n    1\n}", result);
        }

        [Fact]
        public void ShouldNavigateInsideBlockInsteadOfHistory()
        {
            var device = new FakeConsoleDevice();
            var editor = CreateEditor(device);

            device.Enqueue(Key.Type("old"));
            device.Enqueue(Key.Enter);
            Read(editor);

            device.Enqueue(Key.Type("{"));
            device.Enqueue(Key.Enter);
            device.Enqueue(Key.Type("1"));
            device.Enqueue(Key.Press(ConsoleKey.UpArrow));
            device.Enqueue(Key.Press(ConsoleKey.End, ConsoleModifiers.Control));
            device.Enqueue(Key.Type("}"));
            device.Enqueue(Key.Enter);

            Assert.Equal("{\n    1}", Read(editor, text => text.Contains("}")));
        }

        [Fact]
        public void ShouldCancelCurrentLineWithControlC()
        {
            var device = new FakeConsoleDevice();
            device.Enqueue(Key.Type("1+1"));
            device.Enqueue(Key.Ctrl(ConsoleKey.C));

            Assert.Equal(string.Empty, Read(CreateEditor(device)));
        }

        [Fact]
        public void ShouldSignalEndOfInputWithControlDOnEmptyLine()
        {
            var device = new FakeConsoleDevice();
            device.Enqueue(Key.Ctrl(ConsoleKey.D));

            Assert.Null(Read(CreateEditor(device)));
        }

        [Fact]
        public void ShouldKillAndYankLine()
        {
            var device = new FakeConsoleDevice();
            device.Enqueue(Key.Type("hello world"));
            device.Enqueue(Key.Ctrl(ConsoleKey.A));
            device.Enqueue(Key.Ctrl(ConsoleKey.K));
            device.Enqueue(Key.Ctrl(ConsoleKey.Y));
            device.Enqueue(Key.Enter);

            Assert.Equal("hello world", Read(CreateEditor(device)));
        }

        [Fact]
        public void ShouldDeleteWordToTheLeft()
        {
            var device = new FakeConsoleDevice();
            device.Enqueue(Key.Type("foo bar"));
            device.Enqueue(Key.Ctrl(ConsoleKey.W));
            device.Enqueue(Key.Enter);

            Assert.Equal("foo ", Read(CreateEditor(device)));
        }

        [Fact]
        public void ShouldNotClearLineOnASingleEscape()
        {
            var device = new FakeConsoleDevice();
            device.Enqueue(Key.Type("keep"));
            device.Enqueue(Key.Press(ConsoleKey.Escape));
            device.Enqueue(Key.Enter);

            Assert.Equal("keep", Read(CreateEditor(device)));
        }

        [Fact]
        public void ShouldClearLineOnDoubleEscape()
        {
            var device = new FakeConsoleDevice();
            device.Enqueue(Key.Type("noise"));
            device.Enqueue(Key.Press(ConsoleKey.Escape));
            device.Enqueue(Key.Press(ConsoleKey.Escape));
            device.Enqueue(Key.Type("1"));
            device.Enqueue(Key.Enter);

            Assert.Equal("1", Read(CreateEditor(device)));
        }

        [Fact]
        public void ShouldYankBackAClearedLine()
        {
            var device = new FakeConsoleDevice();
            device.Enqueue(Key.Type("expensive input"));
            device.Enqueue(Key.Press(ConsoleKey.Escape));
            device.Enqueue(Key.Press(ConsoleKey.Escape));
            device.Enqueue(Key.Ctrl(ConsoleKey.Y));
            device.Enqueue(Key.Enter);

            Assert.Equal("expensive input", Read(CreateEditor(device)));
        }

        [Fact]
        public void ShouldClearLineWithControlG()
        {
            var device = new FakeConsoleDevice();
            device.Enqueue(Key.Type("noise"));
            device.Enqueue(Key.Ctrl(ConsoleKey.G));
            device.Enqueue(Key.Type("1"));
            device.Enqueue(Key.Enter);

            Assert.Equal("1", Read(CreateEditor(device)));
        }

        [Fact]
        public void ShouldInsertCharactersTypedWithAltGr()
        {
            var device = new FakeConsoleDevice();
            // AltGr+A on a Polish layout: reported as Control|Alt with a printable character.
            device.Enqueue(new ConsoleKeyInfo('ą', ConsoleKey.A, shift: false, alt: true, control: true));
            device.Enqueue(Key.Enter);

            Assert.Equal("ą", Read(CreateEditor(device)));
        }

        [Fact]
        public void ShouldCompleteSingleMatchOnTab()
        {
            var device = new FakeConsoleDevice();
            var editor = CreateEditor(device);
            editor.CompletionProvider = new StaticCompletionProvider("Console", "Contains", "Count");

            device.Enqueue(Key.Type("Cou"));
            device.Enqueue(Key.Tab);
            device.Enqueue(Key.Enter);

            Assert.Equal("Count", Read(editor));
        }

        [Fact]
        public void ShouldCycleThroughCompletionsOnRepeatedTab()
        {
            var device = new FakeConsoleDevice();
            var editor = CreateEditor(device);
            editor.CompletionProvider = new StaticCompletionProvider("Console", "Contains", "Count");

            device.Enqueue(Key.Type("Co"));
            device.Enqueue(Key.Tab);
            device.Enqueue(Key.Tab);
            device.Enqueue(Key.Enter);

            Assert.Equal("Contains", Read(editor));
        }

        [Fact]
        public void ShouldWrapLongInputOntoMultipleRows()
        {
            var device = new FakeConsoleDevice(width: 20, height: 6);
            device.Enqueue(Key.Type("0123456789012345678901234"));
            device.Enqueue(Key.Enter);

            Read(CreateEditor(device));

            // The last column is left unused, so 19 cells are available per row.
            Assert.Equal("> 01234567890123456", device.GetRow(0));
            Assert.Equal("78901234", device.GetRow(1));
        }

        [Fact]
        public void ShouldNotLeaveStaleCharactersBehindWhenTextShrinks()
        {
            var device = new FakeConsoleDevice();
            device.Enqueue(Key.Type("abcdef"));
            device.Enqueue(Key.Press(ConsoleKey.Backspace));
            device.Enqueue(Key.Press(ConsoleKey.Backspace));
            device.Enqueue(Key.Press(ConsoleKey.Backspace));
            device.Enqueue(Key.Enter);

            Assert.Equal("abc", Read(CreateEditor(device)));
            Assert.Equal("> abc", device.GetRow(0));
        }

        [Fact]
        public void ShouldRecallEntryWithReverseSearch()
        {
            var device = new FakeConsoleDevice();
            var editor = CreateEditor(device);

            device.Enqueue(Key.Type("var answer = 42;"));
            device.Enqueue(Key.Enter);
            Read(editor);

            device.Enqueue(Key.Ctrl(ConsoleKey.R));
            device.Enqueue(Key.Type("answ"));
            device.Enqueue(Key.Enter);
            device.Enqueue(Key.Enter);

            Assert.Equal("var answer = 42;", Read(editor));
        }

        [Fact]
        public void ShouldRestoreInputWhenReverseSearchIsCancelled()
        {
            var device = new FakeConsoleDevice();
            var editor = CreateEditor(device);

            device.Enqueue(Key.Type("var answer = 42;"));
            device.Enqueue(Key.Enter);
            Read(editor);

            device.Enqueue(Key.Type("keep"));
            device.Enqueue(Key.Ctrl(ConsoleKey.R));
            device.Enqueue(Key.Type("answ"));
            device.Enqueue(Key.Press(ConsoleKey.Escape));
            device.Enqueue(Key.Enter);

            Assert.Equal("keep", Read(editor));
        }

        [Fact]
        public void ShouldRenderCorrectlyWithColorsEnabled()
        {
            var device = new FakeConsoleDevice { SupportsColors = true };
            device.Enqueue(Key.Type("var x = 1;"));
            device.Enqueue(Key.Enter);

            Assert.Equal("var x = 1;", Read(CreateEditor(device)));
            Assert.Equal("> var x = 1;", device.GetRow(0));
        }

        [Fact]
        public void ShouldKeepIndentationOfPastedBlock()
        {
            var device = new FakeConsoleDevice();
            device.EnqueuePaste("if (true) {\n    Foo();\n}\n");

            var result = Read(CreateEditor(device), BraceCompleteness.IsComplete);

            Assert.Equal("if (true) {\n    Foo();\n}", result);
        }

        [Fact]
        public void ShouldNotRepaintForEveryKeyStrokeWhilePasting()
        {
            var device = new FakeConsoleDevice(width: 80, height: 20);
            device.EnqueuePaste(new string('x', 60) + "\n");

            Read(CreateEditor(device));

            Assert.True(device.WriteCount <= 10, $"Expected a paste to be painted in one go but got {device.WriteCount} writes.");
        }

        [Fact]
        public void ShouldSurviveAFailingCompletionProvider()
        {
            var device = new FakeConsoleDevice();
            var editor = CreateEditor(device);
            editor.CompletionProvider = new ThrowingCompletionProvider();

            device.Enqueue(Key.Type("x"));
            device.Enqueue(Key.Tab);
            device.Enqueue(Key.Enter);

            Assert.Equal("x", Read(editor));
        }

        [Fact]
        public void ShouldIgnoreCompletionSpanOutsideOfTheBuffer()
        {
            var device = new FakeConsoleDevice();
            var editor = CreateEditor(device);
            editor.CompletionProvider = new OutOfRangeCompletionProvider();

            device.Enqueue(Key.Type("x"));
            device.Enqueue(Key.Tab);
            device.Enqueue(Key.Enter);

            Assert.Equal("xitem", Read(editor));
        }
    }
}
