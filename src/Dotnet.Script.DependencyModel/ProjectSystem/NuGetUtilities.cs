using NuGet.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    internal static class NuGetUtilities
    {

        public static string GetNearestConfigPath(string pathToEvaluate)
        {
            var settings = Settings.LoadDefaultSettings(pathToEvaluate);
            return settings.GetConfigFilePaths().FirstOrDefault();
        }

        public static void CreateNuGetConfigFromLocation(string pathToEvaluate, string targetDirectory)
        {
            var sourceSettings = Settings.LoadDefaultSettings(pathToEvaluate);
            var targetSettings = new Settings(targetDirectory);

            CopySection(sourceSettings, targetSettings, "config");
            CopySection(sourceSettings, targetSettings, "bindingRedirects");
            CopySection(sourceSettings, targetSettings, "packageRestore");
            CopySection(sourceSettings, targetSettings, "solution");
            CopySection(sourceSettings, targetSettings, "packageSources");
            CopySection(sourceSettings, targetSettings, "packageSourceCredentials");
            CopySection(sourceSettings, targetSettings, "apikeys");
            CopySection(sourceSettings, targetSettings, "disabledPackageSources");
            CopySection(sourceSettings, targetSettings, "activePackageSource");

            targetSettings.SaveToDisk();
        }

        private static void CopySection(ISettings sourceSettings, ISettings targetSettings, string sectionName)
        {
            var existingAddItems = sourceSettings.GetSection(sectionName)?.Items.Where(item => item is object && (item is SourceItem || item is AddItem) && item.ElementName.ToLowerInvariant() == "add").Cast<AddItem>();

            if (existingAddItems == null)
            {
                return;
            }

            foreach (var addItem in existingAddItems)
            {
                if (ShouldResolvePath(sectionName, addItem.Key))
                {
                    targetSettings.AddOrUpdate(sectionName, new AddItem(addItem.Key, addItem.GetValueAsPath()));
                }
                else
                {
                    targetSettings.AddOrUpdate(sectionName, addItem);
                }
            }
        }

        private static bool ShouldResolvePath(string sectionName, string key)
        {
            if (sectionName == "packageSources")
            {
                return true;
            }

            if (sectionName == "config")
            {
                return key == "globalPackagesFolder" || key == "repositoryPath";
            }

            return false;
        }
    }
}
