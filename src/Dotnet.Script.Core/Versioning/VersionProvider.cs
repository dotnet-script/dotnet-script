using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Dotnet.Script.Core.Versioning
{
    /// <summary>
    /// Provides version information.
    /// </summary>
    public class VersionProvider : IVersionProvider
    {
        private const string UserAgent = "dotnet-script";
        private static readonly string RequestUri = "/repos/dotnet-script/dotnet-script/releases/latest";

        /// <inheritdoc>
        public async Task<VersionInfo> GetLatestVersion()
        {
            using (var httpClient = CreateHttpClient())
            {
                var response = await httpClient.GetStringAsync(RequestUri);
                return ParseTagName(response);
            }

            HttpClient CreateHttpClient()
            {
                var httpClient = new HttpClient { BaseAddress = new Uri("https://api.github.com") };
                httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                return httpClient;
            }

            VersionInfo ParseTagName(string json)
            {
                JsonNode jsonResult = JsonNode.Parse(json);
                return new VersionInfo(jsonResult["tag_name"].GetValue<string>(), isResolved: true);
            }
        }

        /// <inheritdoc>
        public VersionInfo GetCurrentVersion()
        {
            var versionAttribute = typeof(VersionProvider).Assembly.GetCustomAttributes<AssemblyInformationalVersionAttribute>().Single();
            return new VersionInfo(versionAttribute.InformationalVersion, isResolved: true);
        }
    }
}