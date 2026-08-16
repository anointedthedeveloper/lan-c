using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LanServer
{
    public class UdpBeacon
    {
        private bool _running;
        private UdpClient? _listener;

        public void Start()
        {
            _running = true;
            Task.Run(BroadcastLoop);
            Task.Run(ListenLoop);
        }

        public void Stop()
        {
            _running = false;
            try { _listener?.Close(); } catch { }
        }

        // Broadcast beacon every 2 s so clients passively receive it
        private async Task BroadcastLoop()
        {
            using var sender = new UdpClient();
            sender.EnableBroadcast = true;
            var endpoint = new IPEndPoint(IPAddress.Broadcast, Config.Current.UdpPort);

            while (_running)
            {
                try
                {
                    // Refresh message each iteration in case config changed
                    var msg = Encoding.UTF8.GetBytes(
                        $"LANC_SERVER:{Config.Current.WebSocketPort}:{Config.Current.HttpPort}");
                    await sender.SendAsync(msg, msg.Length, endpoint);
                }
                catch { }
                await Task.Delay(2000);
            }
        }

        // Reply to active LANC_DISCOVER probes from clients
        private async Task ListenLoop()
        {
            try
            {
                _listener = new UdpClient();
                _listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listener.Client.Bind(new IPEndPoint(IPAddress.Any, Config.Current.UdpPort));
                _listener.EnableBroadcast = true;

                while (_running)
                {
                    try
                    {
                        var result = await _listener.ReceiveAsync();
                        var msg = Encoding.UTF8.GetString(result.Buffer);
                        if (msg == "LANC_DISCOVER")
                        {
                            var reply = Encoding.UTF8.GetBytes(
                                $"LANC_SERVER:{Config.Current.WebSocketPort}:{Config.Current.HttpPort}");
                            await _listener.SendAsync(reply, reply.Length, result.RemoteEndPoint);
                        }
                    }
                    catch (SocketException) when (!_running) { break; }
                    catch { }
                }
            }
            catch { }
        }
    }
}
