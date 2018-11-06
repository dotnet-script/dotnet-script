using NuGet.Configuration;
using System.Collections.Generic;

namespace Dotnet.Script.Core.Internal
{
    internal static class NuGetUtilities
    {
        struct NuGetConfigSection
        {
            public string Name;
            public HashSet<string> KeysForPathValues;
            public bool AreAllValuesPaths;
        }

        static readonly NuGetConfigSection[] NuGetSections = 
        {
            new NuGetConfigSection { Name = "config", KeysForPathValues = new HashSet<string> { "globalPackagesFolder", "repositoryPath" } },
            new NuGetConfigSection { Name = "bindingRedirects" },
            new NuGetConfigSection { Name = "packageRestore" },
            new NuGetConfigSection { Name = "solution" },
            new NuGetConfigSection { Name = "packageSources", AreAllValuesPaths = true },
            new NuGetConfigSection { Name = "packageSourceCredentials" },
            new NuGetConfigSection { Name = "apikeys" },
            new NuGetConfigSection { Name = "disabledPackageSources" },
            new NuGetConfigSection { Name = "activePackageSource" },
        };

        // Create a NuGet file containing all properties with resolved absolute paths
        public static void CreateNuGetConfigFromLocation(string pathToEvaluate, string directoryToCopy)
        {
            var settings = Settings.LoadDefaultSettings(pathToEvaluate);
            var target = new Settings(directoryToCopy);

            var valuesToSet = new List<SettingValue>();
            foreach (var section in NuGetSections)
            {
                // Resolve properly path values
                valuesToSet.Clear();
                if (section.AreAllValuesPaths)
                {
                    // All values are paths
                    var values = settings.GetSettingValues(section.Name, true);
                    valuesToSet.AddRange(values);
                }
                else
                {
                    var values = settings.GetSettingValues(section.Name, false);
                    if (section.KeysForPathValues != null)
                    {
                        // Some values are path
                        foreach (var value in values)
                        {
                            if (section.KeysForPathValues.Contains(value.Key))
                            {
                                var val = settings.GetValue(section.Name, value.Key, true);
                                value.Value = val;
                            }

                            valuesToSet.Add(value);
                        }
                    }
                    else
                        // All values are not path
                        valuesToSet.AddRange(values);
                }
                target.SetValues(section.Name, valuesToSet);
            }
        }
    }
}
