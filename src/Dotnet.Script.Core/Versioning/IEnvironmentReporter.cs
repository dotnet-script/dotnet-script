using System.Threading.Tasks;

namespace Dotnet.Script.Core.Versioning
{
    /// <summary>
    /// Represents a class that is capable of reporting
    /// environmental information to the <see cref="ScriptConsole"/>.
    /// </summary>
    interface IEnvironmentReporter
    {
         /// <summary>
         /// Reports environmental information to the <see cref="ScriptConsole"/>.
         /// </summary>
         Task ReportInfo();
    }
}