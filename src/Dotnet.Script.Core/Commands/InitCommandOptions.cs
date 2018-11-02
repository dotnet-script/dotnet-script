using System.IO;

namespace Dotnet.Script.Core.Commands
{
    public class InitCommandOptions
    {
        public InitCommandOptions(string fileName, string workingDirectory)
        {
            FileName = fileName;
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory();
        }

        public string FileName { get; }
        public string WorkingDirectory { get; }
    }
}