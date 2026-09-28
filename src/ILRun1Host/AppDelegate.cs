using Foundation;
using UIKit;

namespace WP7ILRun1;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public override UIWindow? Window { get; set; }

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        AppLog.Write("[APP][FINISHED_LAUNCHING_ENTER]");

        try
        {
            var window = new UIWindow(UIScreen.MainScreen.Bounds);
            Window = window;
            window.RootViewController = new MainViewController();
            window.MakeKeyAndVisible();

            AppLog.Write($"[APP][WINDOW_READY] bounds={window.Bounds}");
            AppLog.Write("[APP][ILRUN1_UI_VISIBLE_REQUESTED]");
            AppLog.Write("[APP][FINISHED_LAUNCHING_EXIT] true");
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Write($"[APP][FINISHED_LAUNCHING_FATAL] {ex}");
            throw;
        }
    }

    public override void OnActivated(UIApplication application) =>
        AppLog.Write("[APP][ON_ACTIVATED]");

    public override void WillEnterForeground(UIApplication application) =>
        AppLog.Write("[APP][WILL_ENTER_FOREGROUND]");

    public override void DidEnterBackground(UIApplication application) =>
        AppLog.Write("[APP][DID_ENTER_BACKGROUND]");

    public override void OnResignActivation(UIApplication application) =>
        AppLog.Write("[APP][ON_RESIGN_ACTIVATION]");
}
