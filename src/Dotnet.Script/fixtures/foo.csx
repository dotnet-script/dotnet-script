using Newtonsoft.Json;
using AutoMapper;

Console.WriteLine("hello!");

var test = new { hi = "i'm json!" };
Console.WriteLine(JsonConvert.SerializeObject(test));

Console.WriteLine(typeof(MapperConfiguration));