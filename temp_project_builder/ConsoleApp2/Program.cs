using Microsoft.DotNet.Cli.Utils;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
            //const string projectDirectory = @"C:\Users\sharpiro\Desktop\temp\dotnetinitAutoGen";
            const string projectDirectory = @"C:\Users\sharpiro\Desktop\temp\dotnetinit";
            CommandResult result;

            //var newCommand = Command.CreateDotNet("new", new string[] { "console", "-o", projectDirectory });
            //result = newCommand.Execute();
            //if (result.ExitCode != 0) throw new System.Exception();

            //var buildCommand = Command.CreateDotNet("build", new string[] { projectDirectory });
            //result = buildCommand.Execute();
            //if (result.ExitCode != 0) throw new System.Exception();

            var outDirectory = @"C:\Users\sharpiro\Desktop\temp\tempScript\publish";
            var publishCommand = Command.CreateDotNet("publish", new string[] { projectDirectory, "-c", "Release", "-r", "win10-x64", "-o", outDirectory });
            result = publishCommand.Execute();
            if (result.ExitCode != 0) throw new System.Exception();
        }
    }
}
