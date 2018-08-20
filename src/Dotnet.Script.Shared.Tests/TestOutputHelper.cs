using System.Threading;
using Dotnet.Script.DependencyModel.Logging;
using Xunit.Abstractions;

namespace Dotnet.Script.Shared.Tests
{
    public static class TestOutputHelper
    {
        private static readonly AsyncLocal<CapturedTestOutput> CurrentCapturedTestOutput
            = new AsyncLocal<CapturedTestOutput>();
        
        public static void Capture(this ITestOutputHelper outputHelper, LogLevel minimumLogLevel = LogLevel.Debug)
        {
            CurrentCapturedTestOutput.Value = new CapturedTestOutput(outputHelper, minimumLogLevel);
        }

        public static CapturedTestOutput Current => CurrentCapturedTestOutput.Value;
       
        public static LogFactory CreateTestLogFactory()
        {
            return type => (level, message, exception) =>
            {
#if DEBUG
                if (level >= Current.MinimumLogLevel)
                {
                    Current.TestOutputHelper.WriteLine($"{level,-7} {type}");
                    Current.TestOutputHelper.WriteLine($"       {message}");
                }
#endif
            };
        }
    }

   

    public class CapturedTestOutput
    {
        public CapturedTestOutput(ITestOutputHelper testOutputHelper, LogLevel minimumLogLevel)
        {
            TestOutputHelper = testOutputHelper;
            MinimumLogLevel = minimumLogLevel;
        }

        public ITestOutputHelper TestOutputHelper { get; }
        public LogLevel MinimumLogLevel { get; }
    }
}
