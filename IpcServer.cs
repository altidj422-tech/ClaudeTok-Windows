using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ClaudeTok;

/// <summary>
/// Tiny localhost HTTP-ish IPC server. PowerShell hook scripts hit it with
/// Invoke-WebRequest to trigger show / hide / quit. Bound only to 127.0.0.1
/// so it's never reachable from the network.
/// </summary>
public class IpcServer
{
    public const int Port = 49823;

    private readonly MainWindow _window;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    public IpcServer(MainWindow window)
    {
        _window = window;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Loopback, Port);
        try
        {
            _listener.Start();
            App.Log($"ipc listening on 127.0.0.1:{Port}");
            _ = AcceptLoop(_cts.Token);
        }
        catch (Exception ex)
        {
            App.Log($"ipc start failed: {ex.Message}");
        }
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }
    }

    private async Task AcceptLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener != null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(token);
                _ = HandleClient(client);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { App.Log($"ipc accept err: {ex.Message}"); }
        }
    }

    private async Task HandleClient(TcpClient client)
    {
        try
        {
            using (client)
            using (var stream = client.GetStream())
            {
                var buf = new byte[1024];
                int n = await stream.ReadAsync(buf, 0, buf.Length);
                var request = Encoding.UTF8.GetString(buf, 0, n);

                // Extract path from "GET /show HTTP/1.1"
                var path = "/";
                var firstLine = request.Split('\n')[0];
                var parts = firstLine.Split(' ');
                if (parts.Length >= 2) path = parts[1];

                string body = "ok";
                Application.Current.Dispatcher.Invoke(() =>
                {
                    switch (path.TrimStart('/').ToLowerInvariant())
                    {
                        case "show":
                            _window.ShowOverlay();
                            break;
                        case "hide":
                            _window.HideOverlay();
                            break;
                        case "quit":
                            Application.Current.Shutdown();
                            break;
                        case "status":
                            body = $"{{\"enabled\":{(_window.IsEnabled_ ? "true" : "false")}," +
                                   $"\"paused\":{(_window.IsPausedThisSession ? "true" : "false")}," +
                                   $"\"visible\":{(_window.IsOverlayVisible ? "true" : "false")}}}";
                            break;
                        default:
                            body = "unknown command";
                            break;
                    }
                });

                var resp = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n" +
                           $"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n\r\n{body}";
                var bytes = Encoding.UTF8.GetBytes(resp);
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }
        }
        catch (Exception ex) { App.Log($"ipc handle err: {ex.Message}"); }
    }
}
