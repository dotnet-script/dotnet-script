using System;

namespace Dotnet.Script.Core
{
    public class ScriptFile
    {
        public ScriptFile(string path)
        {
            IsRemote = IsHttpUri(Path);
            if (IsRemote)
            {
                Path = path;
            }
            else
            {
                Path = path.GetRootedPath();
                Directory = System.IO.Path.GetDirectoryName(Path);
            }
        }

        public string Path {get;}

        public string Directory {get;}

        public bool IsRemote {get;}

        private static bool IsHttpUri(string fileName)
        {
            return Uri.TryCreate(fileName, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}

