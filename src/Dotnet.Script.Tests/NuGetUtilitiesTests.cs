using Dotnet.Script.Core.Internal;
using NuGet.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Dotnet.Script.Tests
{
    using SettingsSection = Dictionary<string, Dictionary<string, string>>;
    using SettingsValues = Dictionary<string, string>;

    public class NuGetUtilitiesTests
    {
        public static object[][] Args =
        {
            new object[]
            {
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <config>
        <add key=""dependencyVersion"" value=""Highest"" />
        <add key=""globalPackagesFolder"" value=""{0}\packages"" />
        <add key=""repositoryPath"" value=""..\installed_packages"" />
        <add key=""http_proxy"" value=""http://company-squid:3128@contoso.com"" />
    </config>
</configuration>",
                new SettingsSection
                {
                    {
                        "config",
                        new SettingsValues
                        {
                            { "dependencyVersion", "Highest" },
                            { "globalPackagesFolder", @"{1}\packages" },
                            { "repositoryPath", @"{0}..\installed_packages" },
                            { "http_proxy", @"http://company-squid:3128@contoso.com" },
                        }
                    },
                }
            },
            new object[]
            {
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <bindingRedirects>
        <add key=""skip"" value=""true"" />
    </bindingRedirects>
</configuration>",
                new SettingsSection
                {
                    {
                        "bindingRedirects",
                        new SettingsValues
                        {
                            { "skip", "true" },
                        }
                    },
                }
            },
            new object[]
            {
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <packageRestore>
        <add key=""enabled"" value=""true"" />
        <add key=""automatic"" value=""true"" />
    </packageRestore >
</configuration>",
                new SettingsSection
                {
                    {
                        "packageRestore",
                        new SettingsValues
                        {
                            { "enabled", "true" },
                            { "automatic", "true" },
                        }
                    },
                }
            },
            new object[]
            {
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <solution>
        <add key=""disableSourceControlIntegration"" value=""true"" />
    </solution>
</configuration>",
                new SettingsSection
                {
                    {
                        "solution",
                        new SettingsValues
                        {
                            { "disableSourceControlIntegration", "true" },
                        }
                    },
                }
            },
            new object[]
            {
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <packageSources>
        <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" protocolVersion=""3"" />
        <add key=""Contoso"" value=""https://contoso.com/packages/"" />
        <add key=""Test Source"" value=""{0}\packages"" />
        <add key=""Relative Test Source"" value=""..\packages"" />
    </packageSources>
</configuration>",
                new SettingsSection
                {
                    {
                        "packageSources",
                        new SettingsValues
                        {
                            { "nuget.org", "https://api.nuget.org/v3/index.json" },
                            { "Contoso", "https://contoso.com/packages/" },
                            { "Test Source", "{1}\\packages" },
                            { "Relative Test Source", "{0}..\\packages" },
                        }
                    },
                }
            },
            new object[]
            {
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <apikeys>
        <add key=""https://MyRepo/ES/api/v2/package"" value=""encrypted_api_key"" />
    </apikeys>
</configuration>",
                new SettingsSection
                {
                    {
                        "apikeys",
                        new SettingsValues
                        {
                            { "https://MyRepo/ES/api/v2/package", "encrypted_api_key" },
                        }
                    },
                }
            },
            new object[]
            {
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <disabledPackageSources>
        <add key=""Contoso"" value=""true"" />
    </disabledPackageSources>
</configuration>",
                new SettingsSection
                {
                    {
                        "disabledPackageSources",
                        new SettingsValues
                        {
                            { "Contoso", "true" },
                        }
                    },
                }
            },
            new object[]
            {
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <activePackageSource>
        <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
    </activePackageSource >
</configuration>",
                new SettingsSection
                {
                    {
                        "activePackageSource",
                        new SettingsValues
                        {
                            { "nuget.org", "https://api.nuget.org/v3/index.json" },
                        }
                    },
                }
            },
        };

        [Theory]
        [MemberData(nameof(Args))]
        public void ShouldGenerateEvaluatedNuGetConfigFile(string sourceNuGet, SettingsSection targetSettings)
        {
            using (var projectFolder = new DisposableFolder())
            {
                // Generate files and directories
                var sourceFolder = Path.Combine(projectFolder.Path, "Source");
                var sourceScript = Path.Combine(sourceFolder, "script.cs");
                var targetFolder = Path.Combine(projectFolder.Path, "Target");
                Directory.CreateDirectory(targetFolder);
                Directory.CreateDirectory(sourceFolder);

                var rootTokens = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "c:" : string.Empty;

                var resolvedSourceNuGet = string.Format(sourceNuGet, rootTokens);
                File.WriteAllText(Path.Combine(sourceFolder, "NuGet.config"), resolvedSourceNuGet);

                // Evaluate and generate the NuGet config file
                NuGetUtilities.CreateNuGetConfigFromLocation(sourceScript, targetFolder);

                // Validate the generated NuGet config file
                var targetNuGetPath = Path.Combine(targetFolder, "NuGet.config");
                Assert.True(File.Exists(targetNuGetPath), $"NuGet.config file was not generated at {targetNuGetPath}");

                sourceFolder += "\\";
                var settings = new Settings(targetFolder, "NuGet.config");
                foreach (var expectedSettings in targetSettings)
                {
                    foreach (var expectedSetting in expectedSettings.Value)
                    {
                        var value = settings.GetValue(expectedSettings.Key, expectedSetting.Key);
                        var resolvedExpectedSetting = string.Format(expectedSetting.Value, sourceFolder, rootTokens);
                        Assert.Equal(resolvedExpectedSetting, value);
                    }
                }
            }
        }
    }
}