using CoreGraphics;
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
            Window = new UIWindow(UIScreen.MainScreen.Bounds);

            var controller = new UIViewController();
            controller.View!.BackgroundColor = UIColor.FromRGB(5, 18, 32);

            var title = new UILabel(new CGRect(20, 80, Window.Bounds.Width - 40, 54))
            {
                Text = "WP7 XAP Runner",
                TextColor = UIColor.Cyan,
                Font = UIFont.BoldSystemFontOfSize(30),
                TextAlignment = UITextAlignment.Center,
                AutoresizingMask = UIViewAutoresizing.FlexibleWidth
            };
            controller.View.AddSubview(title);

            var status = new UILabel(new CGRect(20, 150, Window.Bounds.Width - 40, 120))
            {
                Text = "BOOTUI1 PASS\nUIKit host is rendering.\nIL runtime is intentionally disabled in this build.",
                TextColor = UIColor.White,
                Font = UIFont.SystemFontOfSize(17),
                TextAlignment = UITextAlignment.Center,
                Lines = 0,
                AutoresizingMask = UIViewAutoresizing.FlexibleWidth
            };
            controller.View.AddSubview(status);

            Window.RootViewController = controller;
            Window.MakeKeyAndVisible();

            AppLog.Write($"[APP][WINDOW_READY] bounds={Window.Bounds}");
            AppLog.Write("[APP][BOOTUI1_VISIBLE_REQUESTED]");
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
