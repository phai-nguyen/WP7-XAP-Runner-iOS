using Foundation;
using UIKit;

namespace WP7ILRun1;

[Register("WP7RunnerApplication")]
public sealed class RunnerApplication : UIApplication
{
    public RunnerApplication()
    {
        AppLog.Write("[APP][UIAPPLICATION_CTOR]");
    }
}
