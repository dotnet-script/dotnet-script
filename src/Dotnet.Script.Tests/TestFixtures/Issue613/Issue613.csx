#r "nuget: System.Text.Encoding.CodePages, 5.0.0"

public class Script
{
    public static void Run()
    {
        try
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine(ex.ToString());
        }
    }
}

Script.Run();