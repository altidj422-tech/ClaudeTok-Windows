using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ClaudeTok;

/// <summary>
/// Spotify integration on Windows. Two pieces:
///   1. Detect whether Spotify is currently playing — via Windows
///      System Media Transport Controls (SMTC). Async API, available on
///      Windows 10 1809+.
///   2. Pause / resume — by injecting the VK_MEDIA_PLAY_PAUSE virtual
///      key via SendInput. This toggles play/pause for whichever app
///      currently owns the media session. Same approach Spotify's own
///      keyboard shortcuts use.
///
/// We only resume if we were the one who paused (tracked via a flag file).
/// </summary>
public static class SpotifyControl
{
    private const ushort VK_MEDIA_PLAY_PAUSE = 0xB3;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private static string ResumeFlagPath => Path.Combine(Path.GetTempPath(), "claudetok-spotify-resume");

    public static void PauseIfPlaying()
    {
        Task.Run(async () =>
        {
            try
            {
                if (!IsSpotifyRunning()) return;
                var playing = await IsSpotifyPlayingAsync();
                if (!playing) return;
                SendMediaKey();
                File.WriteAllText(ResumeFlagPath, "1");
                App.Log("paused Spotify");
            }
            catch (Exception ex) { App.Log($"spotify pause err: {ex.Message}"); }
        });
    }

    public static void ResumeIfWePaused()
    {
        Task.Run(() =>
        {
            try
            {
                if (!File.Exists(ResumeFlagPath)) return;
                File.Delete(ResumeFlagPath);
                if (!IsSpotifyRunning()) return;
                SendMediaKey();
                App.Log("resumed Spotify");
            }
            catch (Exception ex) { App.Log($"spotify resume err: {ex.Message}"); }
        });
    }

    private static bool IsSpotifyRunning()
    {
        try
        {
            var procs = Process.GetProcessesByName("Spotify");
            return procs.Length > 0;
        }
        catch { return false; }
    }

    /// <summary>
    /// Query SMTC for the current playback status of Spotify specifically.
    /// </summary>
    private static async Task<bool> IsSpotifyPlayingAsync()
    {
        try
        {
            var mgr = await Windows.Media.Control
                .GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            var sessions = mgr.GetSessions();
            foreach (var s in sessions)
            {
                if (s.SourceAppUserModelId.Contains("Spotify", StringComparison.OrdinalIgnoreCase))
                {
                    var info = s.GetPlaybackInfo();
                    return info.PlaybackStatus == Windows.Media.Control
                        .GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                }
            }
        }
        catch { }
        return false;
    }

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte vk, byte scan, uint flags, IntPtr extraInfo);

    private static void SendMediaKey()
    {
        keybd_event((byte)VK_MEDIA_PLAY_PAUSE, 0, 0, IntPtr.Zero);
        keybd_event((byte)VK_MEDIA_PLAY_PAUSE, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
    }
}
