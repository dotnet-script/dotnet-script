using System;
using System.IO;
using Dotnet.Script.DependencyModel.ProjectSystem;

namespace Dotnet.Script.Tests
{
    public class DisposableFolder : IDisposable
    {
        public DisposableFolder()
        {
            var tempFolder = System.IO.Path.GetTempPath();
            this.Path = System.IO.Path.Combine(tempFolder, System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.GetTempFileName()));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            FileUtils.RemoveDirectory(Path);
        }
    }
}