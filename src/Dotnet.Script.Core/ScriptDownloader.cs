using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dotnet.Script.Core
{
    public class ScriptDownloader
    {
        public async Task<string> Download(string uri)
        {
            using (HttpClient client = new HttpClient(new HttpClientHandler
            {
                // Avoid Deflate due to bugs. For more info, see:
                // https://github.com/weblinq/WebLinq/issues/132
                AutomaticDecompression = DecompressionMethods.GZip
            }))
            {
                using (HttpResponseMessage response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    using (HttpContent content = response.Content)
                    {
                        var mediaType = content.Headers.ContentType?.MediaType?.ToLowerInvariant().Trim();
                        switch (mediaType)
                        {
                            case null:
                            case "":
                            case "text/plain":
                                return await content.ReadAsStringAsync();
                            case "application/gzip":
                            case "application/x-gzip":
                                using (var stream = await content.ReadAsStreamAsync())
                                using (var gzip = new GZipStream(stream, CompressionMode.Decompress))
                                using (var reader = new StreamReader(gzip))
                                    return await reader.ReadToEndAsync();
                            default:
                                throw new NotSupportedException($"The media type '{mediaType}' is not supported when executing a script over http/https");
                        }
                    }
                }
            }
        }
    }
}
