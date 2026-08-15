using System.IO;
using Newtonsoft.Json;

namespace LanServer
{
    public class AppConfig
    {
        public string AdminPassword { get; set; } = "admin234";
        public int WebSocketPort { get; set; } = 5000;
        public int HttpPort { get; set; } = 5001;
        public int UdpPort { get; set; } = 5002;
    }

    public static class Config
    {
        private static readonly string _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lanserver.config.json");
        public static AppConfig Current { get; private set; } = Load();

        private static AppConfig Load() =>
            File.Exists(_path) ? JsonConvert.DeserializeObject<AppConfig>(File.ReadAllText(_path)) ?? new AppConfig() : new AppConfig();

        public static void Save() =>
            File.WriteAllText(_path, JsonConvert.SerializeObject(Current, Formatting.Indented));

        public static bool IsFirstLaunch() => !File.Exists(_path);
    }
}
