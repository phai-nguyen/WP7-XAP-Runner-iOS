using UIKit;

namespace WP7ILRun1;

public static class Program
{
    public static void Main(string[] args)
    {
        AppLog.Initialize();
        AppLog.Write("[APP][MAIN_ENTER]");
        AppLog.Write($"[APP][RUNTIME] {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        AppLog.Write($"[APP][OS] {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            AppLog.Write($"[APP][UNHANDLED_EXCEPTION] {e.ExceptionObject}");

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            AppLog.Write($"[APP][UNOBSERVED_TASK] {e.Exception}");
            e.SetObserved();
        };

        AppLog.Write("[APP][UIApplication.Main_BEGIN]");
        UIApplication.Main(args, null, typeof(AppDelegate));
        AppLog.Write("[APP][UIApplication.Main_END]");
    }
}
