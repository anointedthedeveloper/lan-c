using System.Collections.Concurrent;
using Fleck;

namespace LanServer
{
    public class ConnectedClient
    {
        public string Id { get; set; } = "";           // stable: MachineName-MACAddress
        public string ComputerName { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public DateTime LastSeen { get; set; } = DateTime.Now;
        public bool IsOnline { get; set; } = true;
        public IWebSocketConnection? Socket { get; set; }
    }

    public static class ClientManager
    {
        // Key = stable deviceId (MachineName-MAC).  Multiple sockets from same device → single entry.
        private static readonly ConcurrentDictionary<string, ConnectedClient> _clients = new();
        // Secondary map: socket GUID → deviceId, so we can look up on disconnect
        private static readonly ConcurrentDictionary<string, string> _socketToDevice = new();

        public static event Action? ClientsChanged;

        public static void AddOrUpdate(IWebSocketConnection socket, string computerName, string ip, string deviceId)
        {
            // If the same device reconnects with a new socket, close the stale mapping
            var socketKey = socket.ConnectionInfo.Id.ToString();

            // If this device already exists, update it; otherwise add new
            var client = _clients.GetOrAdd(deviceId, _ => new ConnectedClient { Id = deviceId });
            client.ComputerName = computerName;
            client.IpAddress    = ip;
            client.LastSeen     = DateTime.Now;
            client.IsOnline     = true;
            client.Socket       = socket;

            _socketToDevice[socketKey] = deviceId;
            ClientsChanged?.Invoke();
        }

        public static void Remove(IWebSocketConnection socket)
        {
            var socketKey = socket.ConnectionInfo.Id.ToString();
            if (_socketToDevice.TryRemove(socketKey, out var deviceId))
            {
                if (_clients.TryGetValue(deviceId, out var client))
                {
                    client.IsOnline = false;
                    client.Socket   = null;
                    client.LastSeen = DateTime.Now;
                }
            }
            ClientsChanged?.Invoke();
        }

        public static IEnumerable<ConnectedClient> GetAll() => _clients.Values;

        public static IEnumerable<ConnectedClient> GetOnline() => _clients.Values.Where(c => c.IsOnline);

        public static ConnectedClient? GetById(string id) =>
            _clients.TryGetValue(id, out var c) ? c : null;
    }
}
