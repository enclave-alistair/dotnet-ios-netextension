using Foundation;
using UIKit;

namespace Dotnet.Test.Tray;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;

        return base.FinishedLaunching(application, launchOptions);
    }

    protected override MauiApp CreateMauiApp()
    {
        return MauiProgram.CreateMauiApp();
    }

    private static void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs)
    {
        var newExc = new Exception("TaskSchedulerOnUnobservedTaskException", unobservedTaskExceptionEventArgs.Exception);
        LogUnhandledException(newExc);
    }

    private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
    {
        var newExc = new Exception("CurrentDomainOnUnhandledException", unhandledExceptionEventArgs.ExceptionObject as Exception);
        LogUnhandledException(newExc);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "<Pending>")]
    internal static void LogUnhandledException(Exception exception)
    {
        try
        {
            const string errorFileName = "Fatal.log";
            var libraryPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal); // iOS: Environment.SpecialFolder.Resources
            var errorFilePath = Path.Combine(libraryPath, errorFileName);
            var errorMessage = string.Format("Time: {0}\r\nError: Unhandled Exception\r\n{1}", DateTime.Now, exception.ToString());
            File.WriteAllText(errorFilePath, errorMessage);

            // Log to iOS Device Logging.
            CoreFoundation.OSLog.Default.Log(CoreFoundation.OSLogLevel.Error, $"Crash Report: {errorMessage}");
        }
        catch
        {
            // just suppress any error logging exceptions
        }
    }
}
