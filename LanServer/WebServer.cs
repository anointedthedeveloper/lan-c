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
            FleckLog.Level = Fleck.LogLevel.Error;
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
            // Try http://+:port/ first (works without admin on most machines via netsh or Windows defaults)
            // Fall back to localhost if that also fails
            foreach (var prefix in new[] { $"http://+:{Config.Current.HttpPort}/", $"http://localhost:{Config.Current.HttpPort}/" })
            {
                try
                {
                    _http = new HttpListener();
                    _http.Prefixes.Add(prefix);
                    _http.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                    _http.Start();
                    Task.Run(HttpLoop);
                    var scope = prefix.Contains("+") ? "all interfaces" : "localhost only";
                    LogMessage?.Invoke($"HTTP server listening on port {Config.Current.HttpPort} ({scope})");
                    return;
                }
                catch
                {
                    try { _http?.Stop(); } catch { }
                    _http = null;
                }
            }
            LogMessage?.Invoke("HTTP server failed to start.");
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
                    ServeIndexPage(ctx);
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
                        Serve404Page(ctx);
                    }
                }
            }
            catch { }
            finally
            {
                try { ctx.Response.OutputStream.Close(); } catch { }
            }
        }

        private static void ServeIndexPage(HttpListenerContext ctx)
        {
            var files = FileManager.GetFiles();
            var template = LoadAsset("index.html");

            var tableHtml = files.Count == 0
                ? "<div class=\"empty\">No files uploaded yet.</div>"
                : BuildFileTable(files);

            var html = template
                .Replace("{{FILE_COUNT}}", files.Count.ToString())
                .Replace("{{FILE_TABLE}}", tableHtml);

            WriteHtml(ctx, html, 200);
        }

        private static void Serve404Page(HttpListenerContext ctx)
        {
            WriteHtml(ctx, LoadAsset("404.html"), 404);
        }

        private static string BuildFileTable(List<ManagedFile> files)
        {
            var sb = new StringBuilder("<table><thead><tr><th>File</th><th>Size</th><th></th></tr></thead><tbody>");
            foreach (var f in files)
            {
                var ext = Path.GetExtension(f.FileName).TrimStart('.').ToUpper();
                if (string.IsNullOrEmpty(ext)) ext = "FILE";
                var escaped = Uri.EscapeDataString(f.FileName);
                sb.Append($"""<tr><td><span class="file-icon">{ext}</span><span class="file-name">{f.FileName}</span></td><td class="file-size">{f.FileSize / 1024} KB</td><td><a class="dl-btn" href="/{escaped}">&#8595; Download</a></td></tr>""");
            }
            sb.Append("</tbody></table>");
            return sb.ToString();
        }

        private static void WriteHtml(HttpListenerContext ctx, string html, int statusCode)
        {
            var bytes = Encoding.UTF8.GetBytes(html);
            ctx.Response.StatusCode = statusCode;
            ctx.Response.ContentType = "text/html; charset=utf-8";
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.OutputStream.Write(bytes);
        }

        private static string LoadAsset(string fileName)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", fileName);
            return File.Exists(path) ? File.ReadAllText(path) : $"<html><body>{fileName} not found</body></html>";
        }
    }
}
