namespace Dotnet.Script.Shared.Tests
{
    public class ProcessResult
    {
        public ProcessResult(string output, int exitCode, string standardOut, string standardError)
        {
            this.Output = output;
            this.ExitCode = exitCode;
            this.StandardOut = standardOut;
            this.StandardError = standardError;
        }

        public string Output { get; }
        public int ExitCode { get; }
        public string StandardOut { get; }
        public string StandardError { get; }

        public void Deconstruct(out string output, out int exitCode)
        {
            output = this.Output;
            exitCode = this.ExitCode;
        }
    }
}