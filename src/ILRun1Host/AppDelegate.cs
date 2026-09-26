using Foundation;
using UIKit;

namespace WP7ILRun1;

[Register("WP7RunnerAppDelegate")]
public sealed class AppDelegate : UIApplicationDelegate
{
    public override UIWindow? Window { get; set; }

    public AppDelegate()
    {
        AppLog.Write("[APP][APPDELEGATE_CTOR]");
    }

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        AppLog.Write("[APP][FINISHED_LAUNCHING_ENTER]");

        try
        {
            var window = new UIWindow(UIScreen.MainScreen.Bounds);
            var controller = new MainViewController();
            window.RootViewController = controller;
            window.BackgroundColor = UIColor.SystemGray6;
            Window = window;
            window.MakeKeyAndVisible();

            AppLog.Write($"[APP][WINDOW_READY] bounds={window.Bounds}");
            AppLog.Write($"[APP][ROOT_VC] {controller.GetType().FullName}");
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

    public override void WillTerminate(UIApplication application) =>
        AppLog.Write("[APP][WILL_TERMINATE]");
}
