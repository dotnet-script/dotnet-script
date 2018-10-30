using System;
using System.IO;

namespace Dotnet.Script.Core
{
    public class ScriptFile
    {
        public ScriptFile(string path)
        {
            Path = path;
            IsRemote = IsHttpUri(path);

            if (!IsRemote && HasValue && !File.Exists(path))
            {
                throw new Exception($"Couldn't find file '{path}'");
            }

            if (!IsRemote && HasValue)
            {
                Path = path.GetRootedPath();
                Directory = System.IO.Path.GetDirectoryName(Path);
            }
        }

        public string Path {get;}

        public string Directory {get;}

        public bool IsRemote {get;}

        public bool HasValue { get => !string.IsNullOrWhiteSpace(Path); }

        private static bool IsHttpUri(string fileName)
        {
            return Uri.TryCreate(fileName, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}

