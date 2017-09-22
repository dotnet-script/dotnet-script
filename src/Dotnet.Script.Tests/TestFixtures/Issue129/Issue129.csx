#r "nuget:Newtonsoft.Json, 10.0.2"
#r "nuget:Auth0.ManagementApi,4.4.0"
using Newtonsoft.Json;
using Auth0.ManagementApi;

var client = new ManagementApiClient("token", new Uri("https://foo.auth0.com/api/v2/"));

Console.WriteLine(await client.Connections.GetAllAsync("auth0"));