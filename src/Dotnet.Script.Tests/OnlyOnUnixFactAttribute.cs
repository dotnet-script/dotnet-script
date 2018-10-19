using System.Runtime.InteropServices;
using Xunit;


namespace Dotnet.Script.Tests
{
    public class OnlyOnUnixFactAttribute : FactAttribute
    {
        public OnlyOnUnixFactAttribute()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Skip = "Can run only on Linux";
            }
        }
    }
}
