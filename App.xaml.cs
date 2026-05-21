using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace ClaudeTok;

public partial class App : Application
{
    private Mutex? _singleInstance;
    private MainWindow? _mainWindow;
    private TrayMenu? _tray;
    private IpcServer? _ipc;

    public static new App Current => (App)Application.Current;
    public MainWindow MainWindowRef => _mainWindow!;

    public static string LogPath => Path.Combine(Path.GetTempPath(), "claudetok.log");

    public static void Log(string msg)
    {
        try
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
        }
        catch { /* ignore */ }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single-instance enforcement via named mutex
        bool createdNew;
        _singleInstance = new Mutex(true, "Global\\ClaudeTokOverlay", out createdNew);
        if (!createdNew)
        {
            Log("Another instance already running, exiting.");
            Shutdown();
            return;
        }

        bool startHidden = Array.Exists(e.Args, a => a.Equals("--hidden", StringComparison.OrdinalIgnoreCase));
        Log($"=== launched pid={Environment.ProcessId} --hidden={startHidden} ===");

        _mainWindow = new MainWindow(startHidden);
        _mainWindow.Show();

        _tray = new TrayMenu(_mainWindow);
        _ipc = new IpcServer(_mainWindow);
        _ipc.Start();

        // Write PID file so PowerShell scripts can find us
        try
        {
            var pidPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".claude", "extensions", "tiktok-overlay", "overlay.pid");
            Directory.CreateDirectory(Path.GetDirectoryName(pidPath)!);
            File.WriteAllText(pidPath, Environment.ProcessId.ToString());
        }
        catch (Exception ex) { Log($"pid write failed: {ex.Message}"); }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _ipc?.Stop();
        _tray?.Dispose();
        try
        {
            var pidPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".claude", "extensions", "tiktok-overlay", "overlay.pid");
            if (File.Exists(pidPath)) File.Delete(pidPath);
        }
        catch { }
        _singleInstance?.ReleaseMutex();
        base.OnExit(e);
    }
}
