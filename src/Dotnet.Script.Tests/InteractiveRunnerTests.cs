using Dotnet.Script.Core;
using Dotnet.Script.DependencyModel.Runtime;
using Dotnet.Script.DependencyModel.Logging;
using System.Threading.Tasks;
using Xunit;
using System.IO;
using System.Text;

namespace Dotnet.Script.Tests
{
    public class InteractiveRunnerTests
    {
        private InteractiveRunner GetRunner(TextReader writerIn, TextWriter writerOut, TextWriter writerErr)
        {
            var console = new ScriptConsole(writerOut, writerIn, writerErr);
            var logger = new ScriptLogger(console.Error, true);
            var runtimeDependencyResolver = new RuntimeDependencyResolver(type => ((level, message) =>
            {
                if (level == LogLevel.Debug)
                {
                    logger.Verbose(message);
                }
                if (level == LogLevel.Info)
                {
                    logger.Log(message);
                }
            }));

            var compiler = new ScriptCompiler(logger, runtimeDependencyResolver);
            var runner = new InteractiveRunner(compiler, logger, console);
            return runner;
        }

        [Fact]
        public async Task SimpleOutput()
        {
            var commands = new StringBuilder();
            commands.AppendLine("var x = 1;");
            commands.AppendLine();
            commands.AppendLine("x+x");
            commands.AppendLine();
            commands.AppendLine("#exit");

            var reader = new StringReader(commands.ToString());
            var writer = new StringWriter();
            var error = new StringWriter();

            var runner = GetRunner(reader, writer, error);
            await runner.RunLoop("Debug", true);

            var result = writer.ToString();
            Assert.Contains("2", result);
        }
    }
}
