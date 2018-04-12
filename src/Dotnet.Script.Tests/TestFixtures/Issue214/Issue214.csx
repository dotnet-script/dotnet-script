#r "nuget: NodaTime, 2.0.0"
#r "nuget: NodaTime.Serialization.JsonNet, 2.0.0"
#r "nuget: Newtonsoft.Json, 10.0.3"
using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using NodaTime.Serialization.JsonNet;

var converter = NodaConverters.InstantConverter;
Console.WriteLine("Hello World!");