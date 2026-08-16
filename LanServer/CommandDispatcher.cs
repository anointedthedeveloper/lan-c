using Newtonsoft.Json;

namespace LanServer
{
    public static class CommandDispatcher
    {
        public static void SendToAll(string type, object payload)
        {
            var msg = JsonConvert.SerializeObject(new { type, payload });
            foreach (var client in ClientManager.GetOnline())
                client.Socket?.Send(msg);
        }

        public static void SendTo(string clientId, string type, object payload)
        {
            var client = ClientManager.GetById(clientId);
            if (client?.Socket != null)
            {
                var msg = JsonConvert.SerializeObject(new { type, payload });
                client.Socket.Send(msg);
            }
        }

        public static void SendToTargets(IEnumerable<string> clientIds, string type, object payload)
        {
            foreach (var id in clientIds)
                SendTo(id, type, payload);
        }

        public static void IssueInstall(IEnumerable<string> targets, string fileName, string installerType, string downloadUrl)
        {
            var payload = new { fileName, installerType, downloadUrl };
            foreach (var id in targets)
                SendTo(id, "install", payload);
        }

        public static void IssueDownload(IEnumerable<string> targets, string fileName, string downloadUrl)
        {
            var payload = new { fileName, downloadUrl };
            foreach (var id in targets)
                SendTo(id, "download", payload);
        }

        public static void IssueShutdown(IEnumerable<string> targets)
        {
            foreach (var id in targets)
                SendTo(id, "shutdown", new { });
        }

        public static void IssueOpenUrl(IEnumerable<string> targets, string url)
        {
            foreach (var id in targets)
                SendTo(id, "openUrl", new { url });
        }

        public static void IssueAutoDownload(IEnumerable<string> targets, string fileName, string downloadUrl)
        {
            var payload = new { fileName, downloadUrl };
            foreach (var id in targets)
                SendTo(id, "autodownload", payload);
        }

        public static void IssueUpdate(IEnumerable<string> targets, string fileName, string downloadUrl)
        {
            var payload = new { fileName, downloadUrl };
            foreach (var id in targets)
                SendTo(id, "update", payload);
        }

        public static void IssueUninstall(IEnumerable<string> targets)
        {
            foreach (var id in targets)
                SendTo(id, "uninstall", new { });
        }

        public static void IssueWake(IEnumerable<string> targets)
        {
            foreach (var id in targets)
                SendTo(id, "wake", new { });
        }
    }
}
