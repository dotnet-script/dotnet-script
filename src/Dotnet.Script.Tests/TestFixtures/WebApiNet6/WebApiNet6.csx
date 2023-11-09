#r "sdk:Microsoft.NET.Sdk.Web"

using Microsoft.AspNetCore.Builder;

var a = WebApplication.Create();
a.MapGet("/", () => "Hello world");