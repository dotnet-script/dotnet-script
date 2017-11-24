using System;

namespace Dotnet.Script.Core
{
    public class ScriptRuntimeException : Exception
    {
        public ScriptRuntimeException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
