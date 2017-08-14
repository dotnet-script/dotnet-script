using System.Runtime.InteropServices;

namespace Dotnet.Script.Core.Internal
{
    internal static class RuntimeHelper
    {
        internal static string GetRuntimeIdentitifer()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "osx";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "unix";

            return "win";
        }
    }
}