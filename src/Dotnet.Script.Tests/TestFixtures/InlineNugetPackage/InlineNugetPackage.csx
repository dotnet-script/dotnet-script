#r "nuget:AutoMapper, 6.1.1"

#nullable enable
#warning blah
using AutoMapper;

string x = null;
Console.WriteLine(typeof(MapperConfiguration));

void SayHi(string hi)
{
    Console.WriteLine(hi);
}

SayHi(null);