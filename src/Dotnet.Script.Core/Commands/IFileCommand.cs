using System.Threading.Tasks;

namespace Dotnet.Script.Core.Commands
{

    /// <summary>
    /// Represents a class that executes a script file located on disk.
    /// </summary>
    public interface IFileCommand
    {
        Task<TReturn> Run<TReturn, THost>(FileCommandOptions options);
    }
}