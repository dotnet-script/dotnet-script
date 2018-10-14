using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Dotnet.Script.DependencyModel.Logging;
using Newtonsoft.Json.Linq;

namespace Dotnet.Script.Core.Versioning
{
    /// <summary>
    /// Provides version information.
    /// </summary>
    public class VersionProvider : IVersionProvider
    {
        private const string UserAgent = "dotnet-script";
        private static readonly string RequestUri = "/repos/filipw/dotnet-script/releases/latest";

         /// <inheritdoc>
        public async Task<VersionInfo> GetLatestVersion()
        {                       
            using(var httpClient = CreateHttpClient())
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
                JObject jsonResult = JObject.Parse(json);
                return new VersionInfo(jsonResult.SelectToken("tag_name").Value<string>(), isResolved:true);            
            } 
        }

        /// <inheritdoc>
        public VersionInfo GetCurrentVersion()
        {
            var versionAttribute = typeof(VersionProvider).Assembly.GetCustomAttributes<AssemblyInformationalVersionAttribute>().Single();
            var version = Version.Parse(versionAttribute.InformationalVersion);
            return new VersionInfo(versionAttribute.InformationalVersion, isResolved:true);
        }   
    }
}