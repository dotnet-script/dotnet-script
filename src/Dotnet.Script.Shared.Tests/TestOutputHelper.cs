using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Dotnet.Script.DependencyModel.Logging;
using Xunit.Abstractions;

namespace Dotnet.Script.Shared.Tests
{
    public static class TestOutputHelper
    {
        private static readonly AsyncLocal<ITestOutputHelper> CurrentTestOutputHelper
            = new AsyncLocal<ITestOutputHelper>();

        public static void Capture(this ITestOutputHelper outputHelper)
        {
            CurrentTestOutputHelper.Value = outputHelper;
        }

        public static ITestOutputHelper Current => CurrentTestOutputHelper.Value;

        public static LogFactory TestLogFactory => 
            type => (level, message) => Current.WriteLine($"{level} {message}");
    }

        

}
