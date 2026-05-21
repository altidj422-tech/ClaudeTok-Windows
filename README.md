# ClaudeTok (Windows)

The Windows port of the [macOS ClaudeTok overlay](https://github.com/altidj422-tech/ClaudeTok-Extension-). A native Windows app that pops up a TikTok window while [Claude Code](https://claude.com/claude-code) is thinking, and hides it when the response finishes.

Feature parity with the macOS version:
- ✅ Native Windows app (~30 MB self-contained EXE, single file, no installer)
- ✅ Borderless floating WebView2 window, always-on-top
- ✅ Off-screen parking when hidden (so TikTok stays warm — no white-screen reloads)
- ✅ Pauses Spotify when shown, resumes when hidden (via SMTC + media keys)
- ✅ System tray icon with enable / pause / show / hide / quit
- ✅ Single-instance enforcement via named mutex
- ✅ Auto-launch hidden on Claude Code session start, auto-quit on session end

## Requirements

- **Windows 10 (1809+) or Windows 11**
- **WebView2 Runtime** — usually pre-installed on Windows 11; on Windows 10, the installer (`MicrosoftEdgeWebview2Setup.exe`) will be auto-downloaded the first time the app runs if missing.
- **[Claude Code CLI](https://claude.com/claude-code)** installed.

## Install

### From a release zip (recommended)

1. Download `claudetok-win-vX.Y.Z.zip` from [Releases](https://github.com/altidj422-tech/ClaudeTok-Windows/releases).
2. Right-click the zip → **Properties** → check **Unblock** (if shown) → OK.
3. Extract to any folder.
4. **Double-click `install.bat`.** A PowerShell window opens, installs, then pauses with a "press any key" prompt.

   *Or, if you prefer PowerShell directly:* open PowerShell in that folder and run
   ```powershell
   Get-ChildItem -Recurse | Unblock-File
   powershell -ExecutionPolicy Bypass -File .\install.ps1
   ```
5. **Restart Claude Code.** Next prompt → TikTok appears bottom-right.

> ⚠ **Why the `Unblock-File` / `Bypass` step?** Windows blocks `.ps1` files downloaded from the internet by default (the "execution policy is disabled" error). The two commands above unblock the files and run the installer once without changing your system-wide PowerShell policy. The hooks the installer writes use `-ExecutionPolicy Bypass` internally so they Just Work afterward.

### From source (devs)

```powershell
git clone https://github.com/altidj422-tech/ClaudeTok-Windows.git
cd ClaudeTok-Windows
.\build.bat
.\install.bat
```

Requires the **.NET 8 SDK** ([download](https://dotnet.microsoft.com/download)). The `.bat` wrappers handle the PowerShell execution-policy dance for you; if you'd rather invoke the `.ps1` files directly, use `powershell -ExecutionPolicy Bypass -File .\build.ps1` etc.

## First-launch SmartScreen prompt

The exe isn't code-signed (would need a paid Microsoft certificate). On first run, Windows Defender SmartScreen may show:

> *Windows protected your PC*

Click **More info → Run anyway**. Windows remembers your choice.

## System tray

A ClaudeTok icon appears in the Windows system tray (bottom-right, near the clock — you may need to expand the overflow). Right-click for:

- **Enabled** — master switch, persists across restarts
- **Pause until I quit Claude Code** — temporary off, resets on next launch
- **Show Now** / **Hide Now** — manual triggers
- **Quit Overlay** — kills the binary (next Claude Code session relaunches it)

## How it works (vs macOS)

| | macOS | Windows |
|---|---|---|
| Language | Swift + Cocoa | C# + WPF |
| Browser engine | WKWebView | WebView2 (Edge-Chromium) |
| Tray icon | NSStatusItem | System.Windows.Forms.NotifyIcon |
| IPC | UNIX signals (SIGUSR1/2/TERM) | Local TCP on 127.0.0.1:49823 |
| Hook scripts | bash | PowerShell |
| Spotify control | AppleScript (`tell application Spotify`) | Win32 `SendInput` of media keys + SMTC for state |
| Off-screen parking | NSWindow subclass overriding `constrainFrameRect` | Just set `Left = -10000` (Windows doesn't auto-constrain) |
| Visibility-state override | WKUserScript at document-start | WebView2 `AddScriptToExecuteOnDocumentCreatedAsync` |

The Claude Code hook system itself is identical on both OSes — `~/.claude/settings.json` works the same way. The only difference is the hook *commands* (bash vs PowerShell).

## Uninstall

```powershell
# Stop the running app
Get-Process ClaudeTok -ErrorAction SilentlyContinue | Stop-Process -Force

# Remove the install dir
Remove-Item -Recurse -Force "$env:USERPROFILE\.claude\extensions\tiktok-overlay"

# Manually remove the four hook entries from $env:USERPROFILE\.claude\settings.json
```

## License

MIT — see [LICENSE](LICENSE).
