using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LanClient
{
    public class LanWebSocketClient
    {
        private ClientWebSocket? _ws;
        private string _serverIp = "";
        private int _wsPort;
        private bool _running;
        private readonly CommandExecutor _executor = new();

        public event Action<string>? StatusChanged;
        public event Action<string, bool>? CommandCompleted;

        public string ServerIp  => _serverIp;
        public int    HttpPort  { get; private set; }
        public bool   IsConnected => _ws?.State == System.Net.WebSockets.WebSocketState.Open;

        public async Task ConnectAsync(string ip, int wsPort, int httpPort)
        {
            _serverIp = ip;
            _wsPort = wsPort;
            HttpPort = httpPort;
            _running = true;
            await ConnectLoop();
        }

        public void Disconnect()
        {
            _running = false;
            _ws?.Abort();
        }

        private async Task ConnectLoop()
        {
            while (_running)
            {
                try
                {
                    _ws = new ClientWebSocket();
                    await _ws.ConnectAsync(new Uri($"ws://{_serverIp}:{_wsPort}"), CancellationToken.None);
                    StatusChanged?.Invoke("Connected");
                    await Register();
                    await ReceiveLoop();
                }
                catch
                {
                    StatusChanged?.Invoke("Disconnected - retrying...");
                    await Task.Delay(5000);
                }
            }
        }

        private async Task Register()
        {
            var deviceName = Environment.MachineName;
            var deviceId   = GetStableDeviceId();
            var version    = ClientVersionInfo.Current;
            var msg = JsonConvert.SerializeObject(new
            {
                type         = "register",
                computerName = deviceName,
                deviceId,
                version
            });
            await Send(msg);
        }

        /// <summary>
        /// Builds a stable device ID from machine name + first physical MAC address.
        /// Format: MACHINENAME-AABBCCDDEEFF  (always uppercase, no random component).
        /// </summary>
        private static string GetStableDeviceId()
        {
            try
            {
                var mac = System.Net.NetworkInformation.NetworkInterface
                    .GetAllNetworkInterfaces()
                    .Where(n => n.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback
                             && n.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Tunnel
                             && n.OperationalStatus    == System.Net.NetworkInformation.OperationalStatus.Up)
                    .Select(n => n.GetPhysicalAddress().ToString())
                    .FirstOrDefault(m => !string.IsNullOrEmpty(m) && m != "000000000000");

                if (!string.IsNullOrEmpty(mac))
                    return $"{Environment.MachineName.ToUpper()}-{mac.ToUpper()}";
            }
            catch { }
            return $"{Environment.MachineName.ToUpper()}-LOCAL";
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[4096];
            while (_ws!.State == WebSocketState.Open)
            {
                var sb = new StringBuilder();
                WebSocketReceiveResult result;
                do
                {
                    result = await _ws.ReceiveAsync(buffer, CancellationToken.None);
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close) break;
                _ = Task.Run(() => HandleMessage(sb.ToString()));
            }
        }

        private async Task HandleMessage(string raw)
        {
            try
            {
                var obj = JObject.Parse(raw);
                var type = obj["type"]?.ToString();
                var payload = obj["payload"];

                switch (type)
                {
                    case "install":
                        var fileName = payload?["fileName"]?.ToString() ?? "";
                        var installerType = payload?["installerType"]?.ToString() ?? "NSIS";
                        var installUrl = payload?["downloadUrl"]?.ToString() ?? "";
                        await RunWithProgress($"Installing {fileName}", async (form) =>
                        {
                            form.SetStatus("Downloading...");
                            var progress = new Progress<int>(p => { form.SetProgress(p / 2); form.SetStatus($"Downloading... {p}%"); });
                            var result = await _executor.Install(fileName, installUrl, installerType, progress);
                            form.SetDone(result.Success, result.Success ? "Installation complete." : $"Failed: {result.Message}");
                            await SendAck(result.Success ? "install_ok" : "install_fail");
                            CommandCompleted?.Invoke($"Install {fileName}", result.Success);
                            if (result.Success)
                            {
                                await Task.Delay(2000);
                                form.Invoke(form.Close);
                            }
                        });
                        break;

                    case "download":
                        var dlFile = payload?["fileName"]?.ToString() ?? "";
                        var dlUrl = payload?["downloadUrl"]?.ToString() ?? "";
                        await RunWithProgress($"Downloading {dlFile}", async (form) =>
                        {
                            var progress = new Progress<int>(p => { form.SetProgress(p); form.SetStatus($"Downloading... {p}%"); });
                            var result = await _executor.Download(dlFile, dlUrl, progress);
                            form.SetDone(result.Success, result.Success ? "Download complete." : $"Failed: {result.Message}");
                            await SendAck(result.Success ? "download_ok" : "download_fail");
                            CommandCompleted?.Invoke($"Download {dlFile}", result.Success);
                        });
                        break;

                    case "shutdown":
                        await SendAck("shutdown_ack");
                        CommandExecutor.Shutdown();
                        break;

                    case "openUrl":
                        var urlToOpen = payload?["url"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(urlToOpen))
                        {
                            CommandExecutor.OpenUrl(urlToOpen);
                            await SendAck("openUrl_ok");
                            CommandCompleted?.Invoke($"Open URL: {urlToOpen}", true);
                        }
                        break;

                    case "autodownload":
                        // Server is pushing a file — download then execute if it's an .exe installer
                        var adFile = payload?["fileName"]?.ToString() ?? "";
                        var adUrl  = payload?["downloadUrl"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(adFile) && !string.IsNullOrEmpty(adUrl))
                        {
                            await RunWithProgress($"Auto-Download: {adFile}", async (form) =>
                            {
                                var progress = new Progress<int>(p => { form.SetProgress(p); form.SetStatus($"Downloading... {p}%"); });
                                var result   = await _executor.Download(adFile, adUrl, progress);
                                if (!result.Success)
                                {
                                    form.SetDone(false, $"Failed: {result.Message}");
                                    await SendAck("autodownload_fail");
                                    CommandCompleted?.Invoke($"Auto-Download {adFile}", false);
                                    return;
                                }
                                // If it's an .exe, run it silently — it's a self-installing bootstrapper
                                if (adFile.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                                {
                                    form.SetStatus("Installing...");
                                    CommandExecutor.RunSilentInstaller(result.Message);
                                }
                                form.SetDone(true, "Done.");
                                await SendAck("autodownload_ok");
                                CommandCompleted?.Invoke($"Auto-Download {adFile}", true);
                                await Task.Delay(1500);
                                form.Invoke(form.Close);
                            });
                        }
                        break;

                    case "wake":
                        // Server wants us to show the tray icon / re-attach
                        await SendAck("wake_ok");
                        break;

                    case "uninstall":
                        await SendAck("uninstall_ack");
                        CommandExecutor.Uninstall();
                        break;

                    case "update":
                        var updateUrl  = payload?["downloadUrl"]?.ToString() ?? "";
                        var updateFile = payload?["fileName"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(updateUrl))
                        {
                            await RunWithProgress($"Updating LanC Client...", async (form) =>
                            {
                                form.SetStatus("Downloading update...");
                                var progress = new Progress<int>(p => { form.SetProgress(p); form.SetStatus($"Downloading... {p}%"); });
                                var result   = await _executor.Download(updateFile, updateUrl, progress);
                                if (!result.Success) { form.SetDone(false, $"Download failed: {result.Message}"); return; }
                                form.SetStatus("Installing update...");
                                await SendAck("update_ack");
                                CommandCompleted?.Invoke("Update", true);
                                form.SetDone(true, "Update downloaded. Installing...");
                                await Task.Delay(1500);
                                form.Invoke(form.Close);
                                CommandExecutor.RunUpdater(result.Message);
                            });
                        }
                        break;
                }
            }
            catch { }
        }

        private static async Task RunWithProgress(string title, Func<ProgressForm, Task> action)
        {
            ProgressForm? form = null;
            var tcs = new TaskCompletionSource();

            var thread = new Thread(() =>
            {
                form = new ProgressForm(title);
                form.Load += async (s, e) =>
                {
                    await action(form);
                    tcs.TrySetResult();
                };
                Application.Run(form);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            await tcs.Task;
        }

        private async Task SendAck(string result)
        {
            var msg = JsonConvert.SerializeObject(new { type = "ack", result });
            await Send(msg);
        }

        private async Task Send(string msg)
        {
            if (_ws?.State != WebSocketState.Open) return;
            var bytes = Encoding.UTF8.GetBytes(msg);
            await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
