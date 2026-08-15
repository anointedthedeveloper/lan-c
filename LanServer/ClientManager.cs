using System.Collections.Concurrent;
using Fleck;

namespace LanServer
{
    public class ConnectedClient
    {
        public string Id { get; set; } = "";
        public string ComputerName { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public DateTime LastSeen { get; set; } = DateTime.Now;
        public bool IsOnline { get; set; } = true;
        public IWebSocketConnection? Socket { get; set; }
    }

    public static class ClientManager
    {
        private static readonly ConcurrentDictionary<string, ConnectedClient> _clients = new();

        public static event Action? ClientsChanged;

        public static void AddOrUpdate(IWebSocketConnection socket, string computername, string ip)
        {
            var client = new ConnectedClient
            {
                Id = socket.ConnectionInfo.Id.ToString(),
                ComputerName = computername,
                IpAddress = ip,
                LastSeen = DateTime.Now,
                IsOnline = true,
                Socket = socket
            };
            _clients[client.Id] = client;
            ClientsChanged?.Invoke();
        }

        public static void Remove(IWebSocketConnection socket)
        {
            var id = socket.ConnectionInfo.Id.ToString();
            if (_clients.TryGetValue(id, out var client))
            {
                client.IsOnline = false;
                client.Socket = null;
            }
            ClientsChanged?.Invoke();
        }

        public static IEnumerable<ConnectedClient> GetAll() => _clients.Values;

        public static IEnumerable<ConnectedClient> GetOnline() => _clients.Values.Where(c => c.IsOnline);

        public static ConnectedClient? GetById(string id) =>
            _clients.TryGetValue(id, out var c) ? c : null;
    }
}
