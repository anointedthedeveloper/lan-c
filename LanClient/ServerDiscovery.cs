using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LanClient
{
    public class ServerDiscovery
    {
        private const int UdpPort = 5002;
        private bool _running;

        public event Action<string, int, int>? ServerFound; // ip, wsPort, httpPort

        public void Start()
        {
            _running = true;
            Task.Run(ListenLoop);
            Task.Run(DiscoverLoop);
        }

        public void Stop() => _running = false;

        private async Task ListenLoop()
        {
            using var udp = new UdpClient(UdpPort);
            udp.EnableBroadcast = true;
            while (_running)
            {
                try
                {
                    var result = await udp.ReceiveAsync();
                    var msg = Encoding.UTF8.GetString(result.Buffer);
                    ParseAndNotify(msg, result.RemoteEndPoint.Address.ToString());
                }
                catch { await Task.Delay(500); }
            }
        }

        private async Task DiscoverLoop()
        {
            using var udp = new UdpClient();
            udp.EnableBroadcast = true;
            var msg = Encoding.UTF8.GetBytes("LANC_DISCOVER");
            var endpoint = new IPEndPoint(IPAddress.Broadcast, UdpPort);
            while (_running)
            {
                try { await udp.SendAsync(msg, msg.Length, endpoint); }
                catch { }
                await Task.Delay(5000);
            }
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
