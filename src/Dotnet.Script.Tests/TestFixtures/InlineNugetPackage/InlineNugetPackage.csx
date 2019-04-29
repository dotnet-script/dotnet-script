#r "nuget:AutoMapper, 6.1.1"

using AutoMapper;

Console.WriteLine(typeof(MapperConfiguration));

void SayHi(string hi)
{
    Console.WriteLine(hi);
}

SayHi(null);