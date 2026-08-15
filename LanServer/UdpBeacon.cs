using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LanServer
{
    public class UdpBeacon
    {
        private UdpClient? _udp;
        private bool _running;

        public void Start()
        {
            _running = true;
            Task.Run(BroadcastLoop);
            Task.Run(ListenLoop);
        }

        public void Stop()
        {
            _running = false;
            _udp?.Close();
        }

        private async Task BroadcastLoop()
        {
            using var sender = new UdpClient();
            sender.EnableBroadcast = true;
            var endpoint = new IPEndPoint(IPAddress.Broadcast, Config.Current.UdpPort);
            var msg = Encoding.UTF8.GetBytes($"LANC_SERVER:{Config.Current.WebSocketPort}:{Config.Current.HttpPort}");

            while (_running)
            {
                try { await sender.SendAsync(msg, msg.Length, endpoint); }
                catch { }
                await Task.Delay(2000);
            }
        }

        private async Task ListenLoop()
        {
            _udp = new UdpClient(Config.Current.UdpPort);
            while (_running)
            {
                try
                {
                    var result = await _udp.ReceiveAsync();
                    var msg = Encoding.UTF8.GetString(result.Buffer);
                    if (msg == "LANC_DISCOVER")
                    {
                        var reply = Encoding.UTF8.GetBytes($"LANC_SERVER:{Config.Current.WebSocketPort}:{Config.Current.HttpPort}");
                        await _udp.SendAsync(reply, reply.Length, result.RemoteEndPoint);
                    }
                }
                catch { }
            }
        }
    }
}
