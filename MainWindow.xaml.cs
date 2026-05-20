using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace ClaudeTok;

public partial class MainWindow : Window
{
    private const string TikTokUrl = "https://www.tiktok.com/foryou";

    private double _visibleLeft, _visibleTop;
    private const double HiddenLeft = -10000, HiddenTop = -10000;

    public bool IsEnabled_ { get; set; }
    public bool IsPausedThisSession { get; set; }

    public bool IsOverlayVisible => Left > -1000;

    public MainWindow(bool startHidden)
    {
        InitializeComponent();

        // Persisted enabled state via simple text file (the macOS app uses
        // UserDefaults; we use a flat file for portability)
        IsEnabled_ = LoadEnabled();
        IsPausedThisSession = false;

        // Bottom-right of primary screen, 220px margin from right edge
        var screen = System.Windows.Forms.Screen.PrimaryScreen!.WorkingArea;
        _visibleLeft = screen.Right - this.Width - 220;
        _visibleTop = screen.Bottom - this.Height - 20;

        // Start at the right initial position
        if (startHidden)
        {
            Left = HiddenLeft;
            Top = HiddenTop;
        }
        else
        {
            Left = _visibleLeft;
            Top = _visibleTop;
        }

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await InitWebView();
    }

    private async Task InitWebView()
    {
        // Put WebView2 user data in our extension directory so cookies/login persist
        var userDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".claude", "extensions", "tiktok-overlay", "WebView2Data");
        Directory.CreateDirectory(userDataDir);

        var env = await CoreWebView2Environment.CreateAsync(null, userDataDir);
        await WebView.EnsureCoreWebView2Async(env);

        // Inject visibility-state override at document-start. Same fix as macOS:
        // TikTok's React app refuses to bootstrap when document.visibilityState
        // is "hidden", which it reports when the window is off-screen.
        await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
        (function(){
          var visible = function(){ return 'visible'; };
          var notHidden = function(){ return false; };
          try {
            Object.defineProperty(document, 'visibilityState', { get: visible, configurable: true });
            Object.defineProperty(document, 'hidden', { get: notHidden, configurable: true });
            Object.defineProperty(Document.prototype, 'visibilityState', { get: visible, configurable: true });
            Object.defineProperty(Document.prototype, 'hidden', { get: notHidden, configurable: true });
          } catch(e){}
        })();
        ");

        WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        WebView.CoreWebView2.Navigate(TikTokUrl);
        App.Log($"webview navigating to {TikTokUrl}");
    }

    public void ShowOverlay()
    {
        if (!IsEnabled_ || IsPausedThisSession)
        {
            App.Log($"show suppressed enabled={IsEnabled_} paused={IsPausedThisSession}");
            return;
        }
        SpotifyControl.PauseIfPlaying();
        Left = _visibleLeft;
        Top = _visibleTop;
        Topmost = true;
        ResumePlayback();
    }

    public void HideOverlay()
    {
        SpotifyControl.ResumeIfWePaused();
        PausePlayback();
        Left = HiddenLeft;
        Top = HiddenTop;
    }

    private void ResumePlayback()
    {
        try
        {
            WebView.CoreWebView2?.ExecuteScriptAsync(@"
              document.querySelectorAll('video').forEach(function(v){
                try { v.muted=false; v.volume=1; if (v.paused) v.play().catch(function(){}); } catch(e){}
              });
            ");
        }
        catch { }
    }

    private void PausePlayback()
    {
        try
        {
            WebView.CoreWebView2?.ExecuteScriptAsync(@"
              document.querySelectorAll('video').forEach(function(v){
                try { v.pause(); v.muted=true; } catch(e){}
              });
            ");
        }
        catch { }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Closing the window via the X button should hide, not quit
        e.Cancel = true;
        Left = HiddenLeft;
        Top = HiddenTop;
    }

    // ---------- Persisted enabled state ----------

    private static string EnabledFlagPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".claude", "extensions", "tiktok-overlay", "enabled");

    private static bool LoadEnabled()
    {
        try
        {
            return !File.Exists(EnabledFlagPath) || File.ReadAllText(EnabledFlagPath).Trim() != "0";
        }
        catch { return true; }
    }

    public void SaveEnabled()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(EnabledFlagPath)!);
            File.WriteAllText(EnabledFlagPath, IsEnabled_ ? "1" : "0");
        }
        catch { }
    }
}
