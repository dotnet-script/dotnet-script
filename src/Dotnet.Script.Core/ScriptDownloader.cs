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
                        string scriptFile = $"{Path.GetTempFileName()}.csx";                        
                        var code = await content.ReadAsStringAsync();
                        File.WriteAllText(scriptFile, code);
                        return scriptFile;
                    }
                }
            }
        }
    }
}
