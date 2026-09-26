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

            var title = new UILabel(new CGRect(20, 72, Window.Bounds.Width - 40, 54))
            {
                Text = "WP7 XAP Runner",
                TextColor = UIColor.Cyan,
                Font = UIFont.BoldSystemFontOfSize(30),
                TextAlignment = UITextAlignment.Center,
                AutoresizingMask = UIViewAutoresizing.FlexibleWidth
            };
            controller.View.AddSubview(title);

            var status = new UILabel(new CGRect(24, 145, Window.Bounds.Width - 48, 150))
            {
                Text = "ILRUN1-NET9-PAYLOAD2\nUIKit host PASS\nTesting external managed IL…",
                TextColor = UIColor.White,
                Font = UIFont.SystemFontOfSize(17),
                TextAlignment = UITextAlignment.Center,
                Lines = 0,
                AutoresizingMask = UIViewAutoresizing.FlexibleWidth
            };
            controller.View.AddSubview(status);

            var detail = new UITextView(new CGRect(18, 315, Window.Bounds.Width - 36, 300))
            {
                Editable = false,
                BackgroundColor = UIColor.FromRGB(8, 27, 46),
                TextColor = UIColor.White,
                Font = UIFont.FromName("Menlo", 11) ?? UIFont.SystemFontOfSize(11),
                Text = "[UI][READY]\nThe probe will run after the first rendered frame.\n",
                AutoresizingMask = UIViewAutoresizing.FlexibleWidth
            };
            controller.View.AddSubview(detail);

            Window.RootViewController = controller;
            Window.MakeKeyAndVisible();

            AppLog.Write($"[APP][WINDOW_READY] bounds={Window.Bounds}");
            AppLog.Write("[APP][ILRUN1_UI_VISIBLE_REQUESTED]");
            AppLog.Write("[APP][FINISHED_LAUNCHING_EXIT] true");

            _ = Task.Run(async () =>
            {
                await Task.Delay(1200);
                AppLog.Write("[ILRUN1][BACKGROUND_TASK_BEGIN]");

                IlRun1Result result;
                try
                {
                    result = IlRun1Executor.Execute(line =>
                    {
                        BeginInvokeOnMainThread(() =>
                        {
                            detail.Text += line + "\n";
                            detail.ScrollRangeToVisible(new NSRange(detail.Text.Length, 0));
                        });
                    });
                }
                catch (Exception ex)
                {
                    AppLog.Write($"[ILRUN1][BACKGROUND_TASK_FATAL] {ex}");
                    result = new IlRun1Result(false, ex.ToString(), AppLog.TakeThisPath);
                }

                BeginInvokeOnMainThread(() =>
                {
                    status.Text = result.Success
                        ? "ILRUN1 PASS\nExternal managed IL executed on iPhone."
                        : "ILRUN1 FAIL\nSee persistent logs for the exact boundary.";
                    status.TextColor = result.Success ? UIColor.SystemGreen : UIColor.SystemOrange;
                });
            });

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
