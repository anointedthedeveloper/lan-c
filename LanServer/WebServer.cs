using System.Net;
using System.Net.Mime;
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
            OpenFirewallPorts();
            StartWebSocket();
            StartHttp();
        }

        public void Stop()
        {
            try { _ws?.Dispose(); } catch { }
            try { _http?.Stop(); } catch { }
        }

        // ── Firewall ──────────────────────────────────────────────────────────
        private void OpenFirewallPorts()
        {
            // Add inbound rules so other LAN machines can reach HTTP + WS + UDP ports.
            // Silently skips if the rule already exists or netsh fails.
            var ports = new[]
            {
                (Config.Current.HttpPort,      "TCP", "LanC HTTP"),
                (Config.Current.WebSocketPort, "TCP", "LanC WebSocket"),
                (Config.Current.UdpPort,       "UDP", "LanC UDP Beacon")
            };

            foreach (var (port, proto, name) in ports)
            {
                try
                {
                    // Delete stale rule first (ignore errors)
                    Run("netsh", $"advfirewall firewall delete rule name=\"{name}\"");
                    // Add fresh inbound allow rule
                    Run("netsh",
                        $"advfirewall firewall add rule name=\"{name}\" " +
                        $"dir=in action=allow protocol={proto} localport={port}");
                    LogMessage?.Invoke($"Firewall: opened {proto}/{port} ({name})");
                }
                catch (Exception ex)
                {
                    LogMessage?.Invoke($"Firewall rule skipped ({name}): {ex.Message}");
                }
            }
        }

        private static void Run(string exe, string args)
        {
            var psi = new System.Diagnostics.ProcessStartInfo(exe, args)
            {
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            };
            using var p = System.Diagnostics.Process.Start(psi);
            p?.WaitForExit(3000);
        }

        // ── WebSocket ─────────────────────────────────────────────────────────
        private void StartWebSocket()
        {
            FleckLog.Level = Fleck.LogLevel.Error;
            _ws = new WebSocketServer($"ws://0.0.0.0:{Config.Current.WebSocketPort}");
            _ws.Start(socket =>
            {
                socket.OnOpen    = () => LogMessage?.Invoke($"Client connected: {socket.ConnectionInfo.ClientIpAddress}");
                socket.OnClose   = () =>
                {
                    ClientManager.Remove(socket);
                    LogMessage?.Invoke($"Client disconnected: {socket.ConnectionInfo.ClientIpAddress}");
                };
                socket.OnMessage = msg => HandleWsMessage(socket, msg);
                socket.OnError   = ex  => LogMessage?.Invoke($"Socket error: {ex.Message}");
            });
        }

        private void HandleWsMessage(IWebSocketConnection socket, string msg)
        {
            try
            {
                dynamic? data = JsonConvert.DeserializeObject(msg);
                if (data == null) return;
                string type = data.type ?? "";
                if (type == "register")
                {
                    string name     = data.computerName ?? "Unknown";
                    string ip       = socket.ConnectionInfo.ClientIpAddress;
                    string deviceId = data.deviceId ?? $"{name.ToUpper()}-UNKNOWN";
                    ClientManager.AddOrUpdate(socket, name, ip, deviceId);
                    LogMessage?.Invoke($"Registered: {name} ({ip}) [{deviceId}]");
                }
                else if (type == "ack")
                {
                    LogMessage?.Invoke($"ACK from {socket.ConnectionInfo.ClientIpAddress}: {data.result}");
                }
            }
            catch { }
        }

        // ── HTTP ──────────────────────────────────────────────────────────────
        private void StartHttp()
        {
            foreach (var prefix in new[]
            {
                $"http://+:{Config.Current.HttpPort}/",
                $"http://*:{Config.Current.HttpPort}/",
                $"http://localhost:{Config.Current.HttpPort}/"
            })
            {
                try
                {
                    _http = new HttpListener();
                    _http.Prefixes.Add(prefix);
                    _http.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                    _http.IgnoreWriteExceptions = true;
                    _http.Start();
                    Task.Run(HttpLoop);
                    var scope = prefix.Contains("+") || prefix.Contains("*")
                        ? "all interfaces (LAN accessible)"
                        : "localhost only";
                    LogMessage?.Invoke($"HTTP server on port {Config.Current.HttpPort} ({scope})");
                    return;
                }
                catch
                {
                    try { _http?.Stop(); } catch { }
                    _http = null;
                }
            }
            LogMessage?.Invoke("HTTP server failed to start — try running as Administrator.");
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
                // CORS — allow any origin so browsers on other devices can call /api/*
                ctx.Response.AddHeader("Access-Control-Allow-Origin", "*");

                var path = ctx.Request.Url?.AbsolutePath.TrimStart('/') ?? "";

                // Noise suppression
                if (path.Equals("favicon.ico", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith(".well-known", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Response.StatusCode = 204;
                    return;
                }

                // ── /api/files — JSON list for live polling ────────────────────
                if (path.Equals("api/files", StringComparison.OrdinalIgnoreCase))
                {
                    ServeApiFiles(ctx);
                    return;
                }

                // ── /api/events — SSE stream for live push ────────────────────
                if (path.Equals("api/events", StringComparison.OrdinalIgnoreCase))
                {
                    // Just returns immediately with a cache-busted redirect trick;
                    // the HTML side polls /api/files every 3 s instead — simpler & reliable.
                    ctx.Response.StatusCode = 200;
                    WriteJson(ctx, "{\"ok\":true}");
                    return;
                }

                // ── /api/autodownload/upload — POST a file from web browser ──
                if (path.Equals("api/autodownload/upload", StringComparison.OrdinalIgnoreCase)
                    && ctx.Request.HttpMethod == "POST")
                {
                    HandleAutoDownloadUpload(ctx);
                    return;
                }

                // ── /api/autodownload — list of auto-download entries ──────────
                if (path.Equals("api/autodownload", StringComparison.OrdinalIgnoreCase))
                {
                    ServeApiAutoDownload(ctx);
                    return;
                }

                // ── /dl/<shortCode> — short URL auto-download redirect ─────────
                if (path.StartsWith("dl/", StringComparison.OrdinalIgnoreCase))
                {
                    var shortCode = path.Substring(3);
                    ServeShortDownload(ctx, shortCode);
                    return;
                }

                // ── /autodownload — admin upload page ─────────────────────────
                if (path.Equals("autodownload", StringComparison.OrdinalIgnoreCase))
                {
                    ServeAutoDownloadPage(ctx);
                    return;
                }

                // ── Index page ────────────────────────────────────────────────
                if (string.IsNullOrEmpty(path))
                {
                    ServeIndexPage(ctx);
                    return;
                }

                // ── File download ─────────────────────────────────────────────
                var fileName = Uri.UnescapeDataString(path);
                // Prevent path traversal
                var filePath = Path.GetFullPath(Path.Combine(FileManager.UploadsDir, fileName));
                if (!filePath.StartsWith(FileManager.UploadsDir, StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Response.StatusCode = 403;
                    return;
                }

                if (File.Exists(filePath))
                {
                    ServeFile(ctx, filePath, fileName);
                }
                else
                {
                    ctx.Response.StatusCode = 404;
                    WriteHtml(ctx, LoadAsset("404.html"), 404);
                }
            }
            catch { }
            finally
            {
                try { ctx.Response.OutputStream.Close(); } catch { }
            }
        }

        private static void ServeFile(HttpListenerContext ctx, string filePath, string fileName)
        {
            var ext  = Path.GetExtension(fileName).ToLower();
            var mime = ext switch
            {
                ".html" or ".htm" => "text/html",
                ".pdf"            => "application/pdf",
                ".png"            => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif"            => "image/gif",
                ".txt"            => "text/plain",
                ".zip"            => "application/zip",
                ".json"           => "application/json",
                _                 => "application/octet-stream"
            };
            ctx.Response.ContentType = mime;
            ctx.Response.AddHeader("Content-Disposition",
                $"attachment; filename=\"{fileName}\"");
            using var fs = File.OpenRead(filePath);
            ctx.Response.ContentLength64 = fs.Length;
            fs.CopyTo(ctx.Response.OutputStream);
        }

        private static void ServeApiFiles(HttpListenerContext ctx)
        {
            var files = FileManager.GetFiles().Select(f => new
            {
                name    = f.FileName,
                size    = f.FileSize,
                sizeStr = f.FileSize >= 1024 * 1024
                    ? $"{f.FileSize / 1024.0 / 1024.0:F1} MB"
                    : $"{f.FileSize / 1024} KB",
                ext     = Path.GetExtension(f.FileName).TrimStart('.').ToUpper(),
                url     = "/" + Uri.EscapeDataString(f.FileName)
            });
            WriteJson(ctx, JsonConvert.SerializeObject(files));
        }

        private static void ServeIndexPage(HttpListenerContext ctx)
        {
            var files    = FileManager.GetFiles();
            var template = LoadAsset("index.html");
            var tableHtml = files.Count == 0
                ? """
                  <div class="empty">
                    <span class="empty-icon">▤</span>
                    <div class="empty-text">No files uploaded yet.</div>
                    <div class="empty-sub">Upload files from the LanC Server Control Panel.</div>
                  </div>
                  """
                : BuildFileTable(files);

            var html = template
                .Replace("{{FILE_COUNT}}", files.Count.ToString())
                .Replace("{{FILE_TABLE}}", tableHtml)
                .Replace("{{HTTP_PORT}}",  Config.Current.HttpPort.ToString());

            WriteHtml(ctx, html, 200);
        }

        private static string BuildFileTable(List<ManagedFile> files)
        {
            var sb = new StringBuilder();
            sb.Append("<table><thead><tr><th>File</th><th>Size</th><th></th></tr></thead><tbody>");
            foreach (var f in files)
            {
                var ext     = Path.GetExtension(f.FileName).TrimStart('.').ToUpper();
                if (string.IsNullOrEmpty(ext)) ext = "FILE";
                var escaped = Uri.EscapeDataString(f.FileName);
                var sizeStr = f.FileSize >= 1024 * 1024
                    ? $"{f.FileSize / 1024.0 / 1024.0:F1} MB"
                    : $"{f.FileSize / 1024} KB";
                sb.Append($"""
                    <tr>
                      <td><div class="file-cell">
                        <span class="file-badge">{System.Web.HttpUtility.HtmlEncode(ext)}</span>
                        <div>
                          <div class="file-name">{System.Web.HttpUtility.HtmlEncode(f.FileName)}</div>
                          <div class="file-meta">{sizeStr}</div>
                        </div>
                      </div></td>
                      <td class="file-size">{sizeStr}</td>
                      <td><a class="dl-btn" href="/{escaped}">&#8595;&nbsp;Download</a></td>
                    </tr>
                    """);
            }
            sb.Append("</tbody></table>");
            return sb.ToString();
        }

        private static void WriteHtml(HttpListenerContext ctx, string html, int statusCode = 200)
        {
            var bytes = Encoding.UTF8.GetBytes(html);
            ctx.Response.StatusCode      = statusCode;
            ctx.Response.ContentType     = "text/html; charset=utf-8";
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.OutputStream.Write(bytes);
        }

        private static void WriteJson(HttpListenerContext ctx, string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            ctx.Response.StatusCode      = 200;
            ctx.Response.ContentType     = "application/json; charset=utf-8";
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.OutputStream.Write(bytes);
        }

        private static string LoadAsset(string fileName)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", fileName);
            return File.Exists(path)
                ? File.ReadAllText(path)
                : $"<html><body>Asset '{fileName}' not found.</body></html>";
        }

        // ── Auto-Download API ─────────────────────────────────────────────────

        private static void ServeApiAutoDownload(HttpListenerContext ctx)
        {
            var entries = AutoDownloadManager.GetEntries().Select(e => new
            {
                id        = e.ShortCode,
                fileName  = e.FileName,
                shortUrl  = $"/dl/{e.ShortCode}",
                uploadedAt = e.UploadedAt.ToString("MMM dd, yyyy HH:mm")
            });
            WriteJson(ctx, JsonConvert.SerializeObject(entries));
        }

        private void HandleAutoDownloadUpload(HttpListenerContext ctx)
        {
            try
            {
                // Parse multipart form data (simple boundary parser)
                var contentType = ctx.Request.ContentType ?? "";
                int boundaryIdx = contentType.IndexOf("boundary=", StringComparison.OrdinalIgnoreCase);
                if (boundaryIdx < 0) { ctx.Response.StatusCode = 400; WriteJson(ctx, "{\"error\":\"No boundary\"}"); return; }

                var boundary = "--" + contentType.Substring(boundaryIdx + 9).Trim();
                using var ms = new MemoryStream();
                ctx.Request.InputStream.CopyTo(ms);
                var body = ms.ToArray();

                // Find filename and file data in multipart body
                var bodyStr = Encoding.UTF8.GetString(body, 0, Math.Min(body.Length, 2048));
                var fnMatch = System.Text.RegularExpressions.Regex.Match(
                    bodyStr, @"filename=""([^""]+)""", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (!fnMatch.Success) { ctx.Response.StatusCode = 400; WriteJson(ctx, "{\"error\":\"No filename\"}"); return; }

                var fileName = Path.GetFileName(fnMatch.Groups[1].Value);

                // Find double CRLF after headers → file data starts
                var headerEnd = "\r\n\r\n";
                var headerEndBytes = Encoding.UTF8.GetBytes(headerEnd);
                int dataStart = IndexOf(body, headerEndBytes) + headerEndBytes.Length;

                // File data ends before the closing boundary
                var closingBoundary = Encoding.UTF8.GetBytes("\r\n" + boundary + "--");
                int dataEnd = IndexOf(body, closingBoundary, dataStart);
                if (dataEnd < 0) dataEnd = body.Length;

                var fileData = body[dataStart..dataEnd];
                var dest = Path.Combine(FileManager.UploadsDir, fileName);
                File.WriteAllBytes(dest, fileData);

                var code = AutoDownloadManager.Register(fileName);
                LogMessage?.Invoke($"Web upload: {fileName} → /dl/{code}");

                WriteJson(ctx, JsonConvert.SerializeObject(new
                {
                    shortUrl = $"/dl/{code}",
                    fileName,
                    code
                }));
            }
            catch (Exception ex)
            {
                ctx.Response.StatusCode = 500;
                WriteJson(ctx, JsonConvert.SerializeObject(new { error = ex.Message }));
            }
        }

        private static int IndexOf(byte[] haystack, byte[] needle, int start = 0)
        {
            for (int i = start; i <= haystack.Length - needle.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < needle.Length; j++)
                    if (haystack[i + j] != needle[j]) { found = false; break; }
                if (found) return i;
            }
            return -1;
        }

        private static void ServeShortDownload(HttpListenerContext ctx, string shortCode)
        {
            var entry = AutoDownloadManager.GetByCode(shortCode);
            if (entry == null)
            {
                ctx.Response.StatusCode = 404;
                WriteHtml(ctx, LoadAsset("404.html"), 404);
                return;
            }
            var filePath = Path.GetFullPath(Path.Combine(FileManager.UploadsDir, entry.FileName));
            if (!filePath.StartsWith(FileManager.UploadsDir, StringComparison.OrdinalIgnoreCase) || !File.Exists(filePath))
            {
                ctx.Response.StatusCode = 404;
                WriteHtml(ctx, LoadAsset("404.html"), 404);
                return;
            }
            // Force download with original filename
            ctx.Response.AddHeader("Content-Disposition", $"attachment; filename=\"{entry.FileName}\"");
            ServeFile(ctx, filePath, entry.FileName);
        }

        private static void ServeAutoDownloadPage(HttpListenerContext ctx)
        {
            var html = LoadAsset("autodownload.html");
            WriteHtml(ctx, html, 200);
        }
    }
}
