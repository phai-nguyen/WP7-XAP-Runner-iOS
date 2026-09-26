using UIKit;

namespace WP7ILRun1;

public static class Program
{
    public static void Main(string[] args)
    {
        AppLog.Initialize();
        AppLog.Write("[APP][MAIN_ENTER]");
        AppLog.Write("[APP][BUILD] ILRUN1-NET9-ROOTSR");
        AppLog.Write($"[APP][RUNTIME] {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        AppLog.Write($"[APP][OS] {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        AppLog.Write($"[APP][APPDELEGATE_TYPE] {typeof(AppDelegate).AssemblyQualifiedName}");
        AppLog.Write("[APP][UIApplication.Main_BEGIN]");

        UIApplication.Main(args, null, typeof(AppDelegate));

        AppLog.Write("[APP][UIApplication.Main_END]");
    }
}
