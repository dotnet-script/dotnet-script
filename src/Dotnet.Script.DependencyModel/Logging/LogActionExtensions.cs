using System;

namespace Dotnet.Script.DependencyModel.Logging
{
    public static class LogActionExtensions
    {
        public static void Log(this Action<bool, string> logAction, string message)
        {
            logAction(false, message);
        }

        public static void Verbose(this Action<bool, string> logAction, string message)
        {
            logAction(true, message);
        }
    }
}