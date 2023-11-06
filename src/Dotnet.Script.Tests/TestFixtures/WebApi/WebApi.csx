#r "sdk:Microsoft.NET.Sdk.Web"

using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder();
var app = builder.Build();

app.MapGet("/", () => "Hello World!");