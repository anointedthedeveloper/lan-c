using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LanClient
{
    public class ServerDiscovery
    {
        private const int UdpPort = 5002;
        private CancellationTokenSource? _cts;

        public event Action<string, int, int>? ServerFound; // ip, wsPort, httpPort

        public void Start()
        {
            // Cancel any previous run first
            Stop();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            Task.Run(() => ListenLoop(token),   token);
            Task.Run(() => DiscoverLoop(token), token);
        }

        public void Stop()
        {
            try { _cts?.Cancel(); } catch { }
            _cts = null;
        }

        // Listens for the server's broadcast beacon ("LANC_SERVER:ws:http")
        private async Task ListenLoop(CancellationToken token)
        {
            UdpClient? udp = null;
            try
            {
                udp = new UdpClient();
                // Allow sharing the port so server and client can both run on same machine
                udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udp.Client.Bind(new IPEndPoint(IPAddress.Any, UdpPort));
                udp.EnableBroadcast = true;
                udp.Client.ReceiveTimeout = 0;

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        // ReceiveAsync doesn't accept a token, so we use a Task.WhenAny workaround
                        var receiveTask = udp.ReceiveAsync();
                        var cancelTask  = Task.Delay(Timeout.Infinite, token);
                        var done        = await Task.WhenAny(receiveTask, cancelTask);
                        if (done == cancelTask || token.IsCancellationRequested) break;

                        var result = await receiveTask;
                        var msg    = Encoding.UTF8.GetString(result.Buffer);
                        ParseAndNotify(msg, result.RemoteEndPoint.Address.ToString());
                    }
                    catch (OperationCanceledException) { break; }
                    catch { await Task.Delay(500, token).ContinueWith(_ => { }); }
                }
            }
            catch { }
            finally { udp?.Close(); }
        }

        // Actively sends LANC_DISCOVER every 3 seconds
        private async Task DiscoverLoop(CancellationToken token)
        {
            try
            {
                using var udp = new UdpClient();
                udp.EnableBroadcast = true;
                var msg      = Encoding.UTF8.GetBytes("LANC_DISCOVER");
                var endpoint = new IPEndPoint(IPAddress.Broadcast, UdpPort);

                while (!token.IsCancellationRequested)
                {
                    try { await udp.SendAsync(msg, msg.Length, endpoint); }
                    catch { }
                    await Task.Delay(3000, token).ContinueWith(_ => { }); // swallow cancellation
                }
            }
            catch { }
        }

        private void ParseAndNotify(string msg, string ip)
        {
            // format: LANC_SERVER:<wsPort>:<httpPort>
            if (!msg.StartsWith("LANC_SERVER:")) return;
            var parts = msg.Split(':');
            if (parts.Length < 3) return;
            if (int.TryParse(parts[1], out int wsPort) && int.TryParse(parts[2], out int httpPort))
                ServerFound?.Invoke(ip, wsPort, httpPort);
        }
    }
}
