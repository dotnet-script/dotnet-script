Console.WriteLine(IsInDebugMode(typeof(Foo).Assembly));

// https://www.codeproject.com/Tips/323212/Accurate-way-to-tell-if-an-assembly-is-compiled-in
public static bool IsInDebugMode(System.Reflection.Assembly Assembly)
{
    var attributes = Assembly.GetCustomAttributes(typeof(System.Diagnostics.DebuggableAttribute), false);
    if (attributes.Length > 0)
    {
        var debuggable = attributes[0] as System.Diagnostics.DebuggableAttribute;
        if (debuggable != null)
            return (debuggable.DebuggingFlags & System.Diagnostics.DebuggableAttribute.DebuggingModes.Default) == System.Diagnostics.DebuggableAttribute.DebuggingModes.Default;
        else
            return false;
    }
    else
        return false;
}

public class Foo
{
}