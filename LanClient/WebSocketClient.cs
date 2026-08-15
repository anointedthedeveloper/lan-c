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

        public string ServerIp => _serverIp;
        public int HttpPort { get; private set; }

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
            var msg = JsonConvert.SerializeObject(new { type = "register", computerName = Environment.MachineName });
            await Send(msg);
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
