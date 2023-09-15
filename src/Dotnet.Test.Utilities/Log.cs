namespace Dotnet.Test.Utilities;

public static class Log
{
    public static void Write(string message, params object?[] args)
    {
        CoreFoundation.OSLog.Default.Log(string.Format(message, args));
    }

    public static void Write(Exception ex, string message, params object?[] args)
    {
        Write(message, args);
        Write(ex.ToString());
    }
}