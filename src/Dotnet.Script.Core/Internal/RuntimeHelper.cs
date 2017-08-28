using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Dotnet.Script.Core.Internal
{
    internal static class RuntimeHelper
    {
        internal static string GetPlatformIdentifier()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "osx";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "unix";

            return "win";
        }

        internal static string GetProcessArchitecture()
        {
            return RuntimeInformation.ProcessArchitecture.ToString();
        }

        internal static string GetRuntimeIdentifier()
        {
            return Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.GetRuntimeIdentifier();
        }

        internal static string ResolveTargetFramework()
        {
            return Assembly.GetEntryAssembly().GetCustomAttributes()
                .OfType<System.Runtime.Versioning.TargetFrameworkAttribute>()
                .Select(x => x.FrameworkName)
                .FirstOrDefault();
        }
    }
}