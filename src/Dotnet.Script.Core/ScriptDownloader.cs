using System;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Dotnet.Script.Core
{
    public class ScriptDownloader
    {
        public async Task<string> Download(string uri)
        {
            const string plainTextMediaType = "text/plain";
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    response.EnsureSuccessStatusCode();

                    using (HttpContent content = response.Content)
                    {
                        string mediaType = content.Headers.ContentType.MediaType;

                        if (string.IsNullOrWhiteSpace(mediaType) || mediaType.Equals(plainTextMediaType, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return await content.ReadAsStringAsync();
                        }

                        throw new NotSupportedException($"The media type '{mediaType}' is not supported when executing a script over http/https");
                    }
                }
            }
        }
    }
}
