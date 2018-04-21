using System.Diagnostics;

WriteLine("Success");

private static void RunAndWait(Process process)
{
    process.Start();
    process.WaitForExit();
}