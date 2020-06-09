using System.Text;
using System.Threading.Tasks;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;

namespace Dotnet.Script.Core.Versioning
{
    /// <summary>
    /// A class that reports environmental information to the <see cref="ScriptConsole"/>.
    /// </summary>
    public class EnvironmentReporter
    {
        private readonly IVersionProvider _versionProvider;
        private readonly ScriptConsole _scriptConsole;
        private readonly ScriptEnvironment _scriptEnvironment;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentReporter"/> class.
        /// </summary>
        /// <param name="versionProvider">The <see cref="IVersionProvider"/> that is
        /// responsible for providing the current and latest version.</param>
        /// <param name="scriptConsole">The <see cref="ScriptConsole"/> to write to.</param>
        /// <param name="scriptEnvironment">The <see cref="ScriptEnvironment"/> providing environmental information.</param>
        public EnvironmentReporter(IVersionProvider versionProvider, ScriptConsole scriptConsole, ScriptEnvironment scriptEnvironment)
        {
            _versionProvider = versionProvider;
            _scriptConsole = scriptConsole;
            _scriptEnvironment = scriptEnvironment;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentReporter"/> class using the default dependencies.
        /// </summary>
        /// <param name="logFactory">The <see cref="LogFactory"/> to be used for logging.</param>
        public EnvironmentReporter(LogFactory logFactory)
            : this
            (
                new LoggedVersionProvider(logFactory),
                ScriptConsole.Default,
                ScriptEnvironment.Default
            )
        { }

        /// <inheritdoc/>
        public async Task ReportInfo()
        {
            var currentVersion = _versionProvider.GetCurrentVersion();
            var latestVersion = await _versionProvider.GetLatestVersion();

            ReportEnvironmentalInfo(currentVersion);
            if (!latestVersion.Equals(currentVersion) && latestVersion.IsResolved)
            {
                ReportThatNewVersionIsAvailable(latestVersion);
            }
        }

        private void ReportEnvironmentalInfo(VersionInfo installedVersion)
        {
            var netCoreVersion = _scriptEnvironment.NetCoreVersion;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Version             : {installedVersion.Version}");
            sb.AppendLine($"Install location    : {ScriptEnvironment.Default.InstallLocation}");
            sb.AppendLine($"Target framework    : {netCoreVersion.Tfm}");
            sb.AppendLine($".NET Core version   : {netCoreVersion.Version}");
            sb.AppendLine($"Platform identifier : {ScriptEnvironment.Default.PlatformIdentifier}");
            sb.AppendLine($"Runtime identifier  : {ScriptEnvironment.Default.RuntimeIdentifier}");

            _scriptConsole.WriteNormal(sb.ToString());
        }

        private void ReportThatNewVersionIsAvailable(VersionInfo latestVersion)
        {
            var updateInfo = new StringBuilder();
            updateInfo.AppendLine($"Version {latestVersion} is now available");
            updateInfo.AppendLine("Depending on how dotnet-script was installed, execute on of the following commands to update.");
            updateInfo.AppendLine("Global tool : dotnet tool update dotnet-script -g");
            if (ScriptEnvironment.Default.IsWindows)
            {
                updateInfo.AppendLine("Chocolatey  : choco upgrade Dotnet.Script");
                updateInfo.AppendLine("Powershell  : (new-object Net.WebClient).DownloadString(\"https://raw.githubusercontent.com/filipw/dotnet-script/master/install/install.ps\") | iex");
            }
            else
            {
                updateInfo.AppendLine("Bash        : curl -s https://raw.githubusercontent.com/filipw/dotnet-script/master/install/install.sh | bash");
            }

            _scriptConsole.WriteHighlighted(updateInfo.ToString());
        }
    }
}