#!/usr/bin/env dotnet-script
#r "nuget:Microsoft.Extensions.DependencyInjection, 3.0.1"
using Microsoft.Extensions.DependencyInjection;
Console.WriteLine(typeof(IServiceCollection));