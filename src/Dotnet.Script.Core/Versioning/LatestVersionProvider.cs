using System;
using System.Net.Http;
using System.Threading.Tasks;
using Dotnet.Script.DependencyModel.Logging;
using Newtonsoft.Json.Linq;

namespace Dotnet.Script.Core.Versioning
{
    public class LatestVersionProvider : IVersionProvider
    {
        private const string UserAgent = "dotnet-script";
        
        private static readonly string RequestUri = "/repos/filipw/dotnet-script/releases/latest";

        private readonly Logger _logger;
        
        public LatestVersionProvider(LogFactory logFactory)
        {
            _logger = logFactory.CreateLogger<LatestVersionProvider>();
        }
        
        public async Task<string> GetVersion()
        {                       
            using(var httpClient = CreateHttpClient())
            {
                try
                {                    
                    var response = await httpClient.GetStringAsync(RequestUri);
                    return ParseTagName(response);
                }
                catch (System.Exception ex)
                {
                     _logger.Error("Failed to retrieve information about the latest version", ex);
                     throw;
                }                                
            }                                                       
        }

        private static HttpClient CreateHttpClient()
        {
            var httpClient = new HttpClient { BaseAddress = new Uri("https://api.github.com") };
            httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);                    
            return httpClient;
        }

        private static string ParseTagName(string json)
        {
            JObject jsonResult = JObject.Parse(json);
            return jsonResult.SelectToken("tag_name").Value<string>();
        }
    }
}