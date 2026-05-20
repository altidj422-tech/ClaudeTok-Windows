using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace ClaudeTok;

/// <summary>
/// System-tray icon + right-click menu. Mirrors the macOS NSStatusItem menu:
///   - Enabled (checkable)
///   - Pause until I quit (checkable)
///   - Show Now / Hide Now
///   - Quit
/// </summary>
public class TrayMenu : IDisposable
{
    private readonly WinForms.NotifyIcon _icon;
    private readonly WinForms.ContextMenuStrip _menu;
    private readonly WinForms.ToolStripMenuItem _enabledItem;
    private readonly WinForms.ToolStripMenuItem _pauseItem;
    private readonly MainWindow _window;

    public TrayMenu(MainWindow window)
    {
        _window = window;
        _icon = new WinForms.NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "ClaudeTok",
            Visible = true
        };

        _menu = new WinForms.ContextMenuStrip();

        var header = new WinForms.ToolStripLabel("ClaudeTok") { Enabled = false };
        _menu.Items.Add(header);
        _menu.Items.Add(new WinForms.ToolStripSeparator());

        _enabledItem = new WinForms.ToolStripMenuItem("Enabled", null, ToggleEnabled)
        {
            Checked = window.IsEnabled_,
            CheckOnClick = false
        };
        _menu.Items.Add(_enabledItem);

        _pauseItem = new WinForms.ToolStripMenuItem("Pause until I quit Claude Code", null, TogglePause)
        {
            Checked = window.IsPausedThisSession,
            CheckOnClick = false
        };
        _menu.Items.Add(_pauseItem);

        _menu.Items.Add(new WinForms.ToolStripSeparator());

        _menu.Items.Add(new WinForms.ToolStripMenuItem("Show Now", null, (_, _) => window.ShowOverlay()));
        _menu.Items.Add(new WinForms.ToolStripMenuItem("Hide Now", null, (_, _) => window.HideOverlay()));

        _menu.Items.Add(new WinForms.ToolStripSeparator());
        _menu.Items.Add(new WinForms.ToolStripMenuItem("Quit Overlay", null,
            (_, _) => Application.Current.Shutdown()));

        _icon.ContextMenuStrip = _menu;
    }

    private void ToggleEnabled(object? sender, EventArgs e)
    {
        _window.IsEnabled_ = !_window.IsEnabled_;
        _enabledItem.Checked = _window.IsEnabled_;
        _window.SaveEnabled();
        if (!_window.IsEnabled_) _window.HideOverlay();
    }

    private void TogglePause(object? sender, EventArgs e)
    {
        _window.IsPausedThisSession = !_window.IsPausedThisSession;
        _pauseItem.Checked = _window.IsPausedThisSession;
        if (_window.IsPausedThisSession) _window.HideOverlay();
    }

    private Icon LoadIcon()
    {
        // Try the bundled icon file first, fall back to system Question icon
        try
        {
            var asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var icoPath = Path.Combine(asmPath, "icons", "icon.ico");
            if (File.Exists(icoPath)) return new Icon(icoPath);
        }
        catch { }
        return SystemIcons.Application;
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _menu.Dispose();
    }
}
