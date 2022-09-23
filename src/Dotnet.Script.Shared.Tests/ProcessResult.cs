namespace Dotnet.Script.Shared.Tests
{
    public class ProcessResult
    {
        public ProcessResult(string output, int exitcode, string standardOut, string standardError)
        {
            this.output = output;
            this.exitCode = exitCode;
            this.standardOut = standardOut;
            this.standardError = standardError;
        }

        public string output { get; }
        public int exitCode { get; }
        public string standardOut { get; }
        public string standardError { get; }

        public void Deconstruct(out string output, out int exitCode)
        {
            output = this.output;
            exitCode = this.exitCode;
        }
    }
}