using System.IO;
using Xunit;
using Dotnet.Script.DependencyModel.ProjectSystem;

namespace Dotnet.Script.Tests
{
    using System.Linq;

    public class ScriptFilesResolverTests
    {
        [Fact]
        public void ShouldOnlyResolveRootScript()
        {
            using (var rootFolder = new DisposableFolder())
            {
                var rootScript = WriteScript(string.Empty, rootFolder.Path, "Foo.csx");
                WriteScript(string.Empty, rootFolder.Path, "Bar.csx");
                var scriptFilesResolver = new ScriptFilesResolver();

                var files = scriptFilesResolver.GetScriptFiles(rootScript);

                Assert.Single(files);
                Assert.Contains(files, f => f.Contains("Foo.csx"));
                Assert.Contains(files, f => !f.Contains("Bar.csx"));
            }
        }
        [Fact]
        public void ShouldResolveLoadedScriptInRootFolder()
        {
            using (var rootFolder = new DisposableFolder())
            {
                var rootScript = WriteScript("#load \"Bar.csx\"", rootFolder.Path, "Foo.csx");
                WriteScript(string.Empty, rootFolder.Path, "Bar.csx");
                var scriptFilesResolver = new ScriptFilesResolver();

                var files = scriptFilesResolver.GetScriptFiles(rootScript);

                Assert.Equal(2, files.Count);
                Assert.Equal(new[] { "Foo.csx", "Bar.csx" }, files.Select(Path.GetFileName));
            }
        }

        [Fact]
        public void ShouldResolveLoadedScriptInSubFolder()
        {
            using (var rootFolder = new DisposableFolder())
            {
                var rootScript = WriteScript("#load \"SubFolder/Bar.csx\"", rootFolder.Path, "Foo.csx");
                var subFolder = Path.Combine(rootFolder.Path, "SubFolder");
                Directory.CreateDirectory(subFolder);
                WriteScript(string.Empty, subFolder, "Bar.csx");

                var scriptFilesResolver = new ScriptFilesResolver();
                var files = scriptFilesResolver.GetScriptFiles(rootScript);

                Assert.Equal(2, files.Count);
                Assert.Equal(new[] { "Foo.csx", "Bar.csx" }, files.Select(Path.GetFileName));
            }
        }

        [Fact]
        public void ShouldResolveLoadedScriptWithRootPath()
        {
            using (var rootFolder = new DisposableFolder())
            {
                var subFolder = Path.Combine(rootFolder.Path, "SubFolder");
                Directory.CreateDirectory(subFolder);
                var fullPathToBarScript = WriteScript(string.Empty, subFolder, "Bar.csx");
                var rootScript = WriteScript($"#load \"{fullPathToBarScript}\"", rootFolder.Path, "Foo.csx");


                var scriptFilesResolver = new ScriptFilesResolver();
                var files = scriptFilesResolver.GetScriptFiles(rootScript);

                Assert.Equal(2, files.Count);
                Assert.Equal(new[] { "Foo.csx", "Bar.csx" }, files.Select(Path.GetFileName));
            }
        }


        private static string WriteScript(string content, string folder, string name)
        {
            var fullPath = Path.Combine(folder, name);
            File.WriteAllText(Path.Combine(folder, name), content);
            return fullPath;

        }
    }
}
