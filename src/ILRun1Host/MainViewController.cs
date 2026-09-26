using CoreGraphics;
using UIKit;

namespace WP7ILRun1;

internal sealed class MainViewController : UIViewController
{
    private UITextView? _logView;
    private UILabel? _statusLabel;
    private UIButton? _runButton;
    private bool _ranOnce;
    private bool _running;

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        AppLog.Write("[UI][VIEW_DID_LOAD_ENTER]");

        if (View is null)
        {
            AppLog.Write("[UI][VIEW_NULL]");
            return;
        }

        var view = View;
        view.BackgroundColor = UIColor.SystemGray6;

        var title = new UILabel(new CGRect(16, 54, view.Bounds.Width - 32, 34))
        {
            Text = "WP7 XAP Runner — ILRUN1",
            TextColor = UIColor.Label,
            Font = UIFont.BoldSystemFontOfSize(22),
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth
        };
        view.AddSubview(title);

        _statusLabel = new UILabel(new CGRect(16, 94, view.Bounds.Width - 32, 28))
        {
            Text = "UI READY — chuẩn bị chạy ILRUN1",
            TextColor = UIColor.Label,
            Font = UIFont.SystemFontOfSize(15),
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth
        };
        view.AddSubview(_statusLabel);

        _logView = new UITextView(new CGRect(12, 130, view.Bounds.Width - 24, view.Bounds.Height - 250))
        {
            Editable = false,
            BackgroundColor = UIColor.SystemBackground,
            TextColor = UIColor.Label,
            Font = UIFont.FromName("Menlo", 11) ?? UIFont.SystemFontOfSize(11),
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
            Text = "[UI][READY]\nLog vẫn được ghi vào Files dù ILRUN1 bị treo.\n"
        };
        view.AddSubview(_logView);

        _runButton = UIButton.FromType(UIButtonType.System);
        _runButton.Frame = new CGRect(16, view.Bounds.Height - 104, (view.Bounds.Width - 44) / 2, 44);
        _runButton.SetTitle("Chạy lại IL", UIControlState.Normal);
        _runButton.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleRightMargin;
        _runButton.TouchUpInside += (_, _) => StartProbe();
        view.AddSubview(_runButton);

        var copyButton = UIButton.FromType(UIButtonType.System);
        copyButton.Frame = new CGRect(28 + (view.Bounds.Width - 44) / 2, view.Bounds.Height - 104, (view.Bounds.Width - 44) / 2, 44);
        copyButton.SetTitle("Sao chép log", UIControlState.Normal);
        copyButton.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleLeftMargin;
        copyButton.TouchUpInside += (_, _) =>
        {
            UIPasteboard.General.String = _logView?.Text ?? string.Empty;
            if (_statusLabel is not null)
                _statusLabel.Text = "Đã sao chép log";
            AppLog.Write("[UI][COPY_LOG]");
        };
        view.AddSubview(copyButton);

        AppLog.Write("[UI][VIEW_DID_LOAD_EXIT]");
    }

    public override void ViewDidAppear(bool animated)
    {
        base.ViewDidAppear(animated);
        AppLog.Write("[UI][VIEW_DID_APPEAR]");

        if (_ranOnce)
            return;

        _ranOnce = true;

        // Give UIKit at least one rendered frame before starting the risky runtime probe.
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            BeginInvokeOnMainThread(StartProbe);
        });
    }

    private void StartProbe()
    {
        if (_running || _logView is null || _statusLabel is null)
            return;

        _running = true;
        _runButton?.SetTitle("Đang chạy…", UIControlState.Normal);
        _runButton!.Enabled = false;
        _statusLabel.Text = "ILRUN1 đang chạy nền…";
        _logView.Text += "[UI][PROBE_BACKGROUND_START]\n";
        AppLog.Write("[UI][PROBE_BACKGROUND_START]");

        _ = Task.Run(async () =>
        {
            foreach (var seconds in new[] { 5, 15, 30 })
            {
                await Task.Delay(TimeSpan.FromSeconds(seconds == 5 ? 5 : seconds == 15 ? 10 : 15));
                if (!_running)
                    return;

                AppLog.Write($"[ILRUN1][WATCHDOG] probe_still_running seconds={seconds}");
            }
        });

        _ = Task.Run(() =>
        {
            IlRun1Result result;

            try
            {
                result = IlRun1Executor.Execute(line =>
                {
                    BeginInvokeOnMainThread(() =>
                    {
                        if (_logView is null)
                            return;

                        _logView.Text += line + "\n";
                        _logView.ScrollRangeToVisible(new Foundation.NSRange(_logView.Text.Length, 0));
                    });
                });
            }
            catch (Exception ex)
            {
                AppLog.Write($"[UI][PROBE_TASK_FATAL] {ex}");
                result = new IlRun1Result(false, ex.ToString(), AppLog.TakeThisPath);
            }

            BeginInvokeOnMainThread(() =>
            {
                _running = false;
                if (_runButton is not null)
                {
                    _runButton.Enabled = true;
                    _runButton.SetTitle("Chạy lại IL", UIControlState.Normal);
                }

                if (_statusLabel is not null)
                    _statusLabel.Text = result.Success
                        ? "PASS — external managed IL đã chạy"
                        : "FAIL/HANG boundary — lấy log trong Files";
            });
        });
    }
}
