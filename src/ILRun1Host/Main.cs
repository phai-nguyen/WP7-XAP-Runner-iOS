using UIKit;

namespace WP7ILRun1;

public static class Program
{
    public static void Main(string[] args)
    {
        AppLog.Initialize();
        AppLog.Write("[APP][MAIN_ENTER]");
        AppLog.Write("[APP][BUILD] ILRUN1-IOS3-AOTHOST");
        AppLog.Write($"[APP][RUNTIME] {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        AppLog.Write($"[APP][OS] {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        AppLog.Write($"[APP][APPDELEGATE_TYPE] {typeof(AppDelegate).AssemblyQualifiedName}");
        AppLog.Write($"[APP][PRINCIPAL_TYPE] {typeof(RunnerApplication).AssemblyQualifiedName}");

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            AppLog.Write($"[APP][UNHANDLED_EXCEPTION] {e.ExceptionObject}");

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            AppLog.Write($"[APP][UNOBSERVED_TASK] {e.Exception}");
            e.SetObserved();
        };

        AppLog.Write("[APP][UIApplication.Main_BEGIN]");
        UIApplication.Main(args, typeof(RunnerApplication), typeof(AppDelegate));
        AppLog.Write("[APP][UIApplication.Main_END]");
    }
}
