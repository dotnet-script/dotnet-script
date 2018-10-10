using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Dotnet.Script.Core.Versioning
{
    public class InstalledVersionProvider : IVersionProvider
    {
        public Task<string> GetVersion()
        {
            var versionAttribute = typeof(InstalledVersionProvider).Assembly.GetCustomAttributes<AssemblyInformationalVersionAttribute>().Single();
            var version = Version.Parse(versionAttribute.InformationalVersion);
            return Task.FromResult(versionAttribute.InformationalVersion);
        }
    }
}