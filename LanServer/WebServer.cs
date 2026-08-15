using System.Net;
using System.Text;
using Fleck;
using Newtonsoft.Json;

namespace LanServer
{
    public class WebServer
    {
        private HttpListener? _http;
        private WebSocketServer? _ws;

        public event Action<string>? LogMessage;

        public void Start()
        {
            StartWebSocket();
            StartHttp();
        }

        public void Stop()
        {
            try { _ws?.Dispose(); } catch { }
            try { _http?.Stop(); } catch { }
        }

        private void StartWebSocket()
        {
            FleckLog.Level = LogLevel.Off;
            _ws = new WebSocketServer($"ws://0.0.0.0:{Config.Current.WebSocketPort}");
            _ws.Start(socket =>
            {
                socket.OnOpen = () => LogMessage?.Invoke($"Client connected: {socket.ConnectionInfo.ClientIpAddress}");
                socket.OnClose = () =>
                {
                    ClientManager.Remove(socket);
                    LogMessage?.Invoke($"Client disconnected: {socket.ConnectionInfo.ClientIpAddress}");
                };
                socket.OnMessage = msg => HandleMessage(socket, msg);
                socket.OnError = ex => LogMessage?.Invoke($"Socket error: {ex.Message}");
            });
        }

        private void HandleMessage(IWebSocketConnection socket, string msg)
        {
            try
            {
                dynamic? data = JsonConvert.DeserializeObject(msg);
                if (data == null) return;
                string type = data.type ?? "";
                if (type == "register")
                {
                    string name = data.computerName ?? "Unknown";
                    string ip = socket.ConnectionInfo.ClientIpAddress;
                    ClientManager.AddOrUpdate(socket, name, ip);
                    LogMessage?.Invoke($"Registered: {name} ({ip})");
                }
                else if (type == "ack")
                {
                    LogMessage?.Invoke($"ACK from {socket.ConnectionInfo.ClientIpAddress}: {data.result}");
                }
            }
            catch { }
        }

        private void StartHttp()
        {
            _http = new HttpListener();
            // Use * to listen on all interfaces - run server as admin or use netsh to allow
            _http.Prefixes.Add($"http://*:{Config.Current.HttpPort}/");
            try
            {
                _http.Start();
                Task.Run(HttpLoop);
                LogMessage?.Invoke($"HTTP server listening on port {Config.Current.HttpPort}");
            }
            catch
            {
                // Fallback to localhost only
                _http = new HttpListener();
                _http.Prefixes.Add($"http://localhost:{Config.Current.HttpPort}/");
                _http.Start();
                Task.Run(HttpLoop);
                LogMessage?.Invoke($"HTTP server on localhost:{Config.Current.HttpPort} (limited - run as admin for LAN access)");
            }
        }

        private async Task HttpLoop()
        {
            while (_http!.IsListening)
            {
                try
                {
                    var ctx = await _http.GetContextAsync();
                    _ = Task.Run(() => HandleHttp(ctx));
                }
                catch { }
            }
        }

        private void HandleHttp(HttpListenerContext ctx)
        {
            try
            {
                var path = ctx.Request.Url?.AbsolutePath.TrimStart('/') ?? "";
                if (string.IsNullOrEmpty(path))
                {
                    var files = FileManager.GetFiles();
                    var sb = new StringBuilder("<!DOCTYPE html><html><head><style>body{font-family:Segoe UI;background:#1e1e1e;color:#fff;padding:20px}a{color:#4fc3f7}ul{list-style:none;padding:0}li{padding:8px;border-bottom:1px solid #333}</style></head><body><h2>LanC File Server</h2><ul>");
                    foreach (var f in files)
                        sb.Append($"<li><a href='/{Uri.EscapeDataString(f.FileName)}'>{f.FileName}</a> &nbsp; <span style='color:#aaa'>{f.FileSize / 1024} KB</span></li>");
                    sb.Append("</ul></body></html>");
                    var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                    ctx.Response.ContentType = "text/html; charset=utf-8";
                    ctx.Response.ContentLength64 = bytes.Length;
                    ctx.Response.OutputStream.Write(bytes);
                }
                else
                {
                    var fileName = Uri.UnescapeDataString(path);
                    var filePath = Path.Combine(FileManager.UploadsDir, fileName);
                    if (File.Exists(filePath))
                    {
                        ctx.Response.ContentType = "application/octet-stream";
                        ctx.Response.AddHeader("Content-Disposition", $"attachment; filename=\"{fileName}\"");
                        using var fs = File.OpenRead(filePath);
                        ctx.Response.ContentLength64 = fs.Length;
                        fs.CopyTo(ctx.Response.OutputStream);
                    }
                    else
                    {
                        ctx.Response.StatusCode = 404;
                    }
                }
            }
            catch { }
            finally
            {
                try { ctx.Response.OutputStream.Close(); } catch { }
            }
        }
    }
}
