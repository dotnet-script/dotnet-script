#r "sdk:Microsoft.NET.Sdk.Web"
#r "nuget:Microsoft.Extensions.DependencyInjection.Abstractions, 8.0.0-rc.2.23479.6"

using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder();
var app = builder.Build();

app.MapGet("/", () => "Hello World!");