using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dotnet.Script.Core
{
    public class ScriptDownloader
    {
        public async Task<string> Download(string uri)
        {
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    response.EnsureSuccessStatusCode();
                    using (HttpContent content = response.Content)
                    {
                        return await content.ReadAsStringAsync();
                    }
                }
            }
        }
    }
}
