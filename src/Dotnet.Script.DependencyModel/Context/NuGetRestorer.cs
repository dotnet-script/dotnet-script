using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Process;

namespace Dotnet.Script.DependencyModel.Context
{
    public class NuGetRestorer : IRestorer
    {
        private readonly CommandRunner _commandRunner;
        private readonly Logger _logger;
        private readonly ScriptEnvironment _scriptEnvironment;
        private static readonly string PathToNuget;        

        static NuGetRestorer()
        {
            var directory = Path.GetDirectoryName(new Uri(typeof(NuGetRestorer).GetTypeInfo().Assembly.CodeBase).LocalPath);
            PathToNuget = Path.Combine(directory, "NuGet430.exe");            
        }

        public NuGetRestorer(CommandRunner commandRunner, LogFactory logFactory)
        {
            _commandRunner = commandRunner;
            _logger = logFactory.CreateLogger<NuGetRestorer>();
            _scriptEnvironment = ScriptEnvironment.Default;
        }

        public void Restore(string pathToProjectFile, string[] packageSources)
        {
            ExtractNugetExecutable();
            var packageSourcesArgument = CreatePackageSourcesArguments();
            int exitcode = 0;
            if (_scriptEnvironment.IsWindows)
            {
                exitcode = _commandRunner.Execute(PathToNuget, $"restore {pathToProjectFile} {packageSourcesArgument}");
            }
            else
            {
                exitcode =_commandRunner.Execute("mono", $"{PathToNuget} restore \"{pathToProjectFile}\" {packageSourcesArgument}");
            }

            if (exitcode != 0)
            {
                // We must throw here, otherwise we may incorrectly run with the old 'project.assets.json'
                throw new Exception($"Unable to restore packages from '{pathToProjectFile}'. Make sure that all script files contains valid NuGet references");
            }

            string CreatePackageSourcesArguments()
            {
                return packageSources.Length == 0
                    ? string.Empty
                    : packageSources.Select(s => $"-Source {s}")
                        .Aggregate((current, next) => $"{current} {next}");
            }
        }

        public bool CanRestore => CheckAvailability();

        private bool CheckAvailability()
        {
            if (_scriptEnvironment.IsWindows)
            {
                return _commandRunner.Execute(PathToNuget) == 0;
            }

            return _commandRunner.Execute("mono", PathToNuget) == 0;
        }

        private void ExtractNugetExecutable()
        {
            if (!File.Exists(PathToNuget))
            {
                _logger.Debug("Extracting NuGet executable");
                using (Stream input = typeof(NuGetRestorer).GetTypeInfo().Assembly.GetManifestResourceStream("Dotnet.Script.DependencyModel.NuGet.NuGet430.exe"))
                {

                    byte[] byteData = StreamToBytes(input);
                    File.WriteAllBytes(PathToNuget, byteData);
                }
            }
        }

        private static byte[] StreamToBytes(Stream input)
        {

            int capacity = input.CanSeek ? (int)input.Length : 0; //Bitwise operator - If can seek, Capacity becomes Length, else becomes 0.
            using (MemoryStream output = new MemoryStream(capacity)) //Using the MemoryStream output, with the given capacity.
            {
                int readLength;
                byte[] buffer = new byte[capacity/*4096*/];  //An array of bytes
                do
                {
                    readLength = input.Read(buffer, 0, buffer.Length);   //Read the memory data, into the buffer
                    output.Write(buffer, 0, readLength); //Write the buffer to the output MemoryStream incrementally.
                }
                while (readLength != 0); //Do all this while the readLength is not 0
                return output.ToArray();  //When finished, return the finished MemoryStream object as an array.
            }
        }
    }
}