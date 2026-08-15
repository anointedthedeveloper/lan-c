namespace LanServer
{
    public enum LogLevel { Info, Success, Warning, Error, Debug }

    public class LogEntry
    {
        public DateTime Time     { get; init; } = DateTime.Now;
        public LogLevel Level    { get; init; } = LogLevel.Info;
        public string   Message  { get; init; } = "";
    }

    public class DeploymentRecord
    {
        public string   Id            { get; init; } = Guid.NewGuid().ToString();
        public string   FileName      { get; set; }  = "";
        public string   InstallerType { get; set; }  = "";
        public long     FileSize      { get; set; }
        public DateTime DeployedAt    { get; init; } = DateTime.Now;
        public string   Status        { get; set; }  = "Active";
        public List<string> TargetIds { get; init; } = new();
    }

    public static class AppState
    {
        public static readonly DateTime StartTime = DateTime.Now;

        // ── Logs ──────────────────────────────────────────────────────────────
        private static readonly List<LogEntry> _logs = new();
        private static readonly object _logLock = new();
        public static event Action<LogEntry>? LogAdded;

        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            var entry = new LogEntry { Time = DateTime.Now, Level = level, Message = message };
            lock (_logLock) _logs.Add(entry);
            LogAdded?.Invoke(entry);
        }

        public static IReadOnlyList<LogEntry> GetLogs()
        {
            lock (_logLock) return _logs.ToList();
        }

        public static void ClearLogs()
        {
            lock (_logLock) _logs.Clear();
        }

        // ── Deployments ───────────────────────────────────────────────────────
        private static readonly List<DeploymentRecord> _deployments = new();
        private static readonly object _depLock = new();
        public static event Action? DeploymentsChanged;

        public static void AddDeployment(DeploymentRecord rec)
        {
            lock (_depLock) _deployments.Add(rec);
            DeploymentsChanged?.Invoke();
        }

        public static void RemoveDeployment(string id)
        {
            lock (_depLock) _deployments.RemoveAll(d => d.Id == id);
            DeploymentsChanged?.Invoke();
        }

        public static IReadOnlyList<DeploymentRecord> GetDeployments()
        {
            lock (_depLock) return _deployments.ToList();
        }

        // ── Uptime ────────────────────────────────────────────────────────────
        public static TimeSpan Uptime => DateTime.Now - StartTime;

        public static string UptimeString
        {
            get
            {
                var u = Uptime;
                return u.TotalHours >= 1
                    ? $"{(int)u.TotalHours:D2}:{u.Minutes:D2}:{u.Seconds:D2}"
                    : $"{u.Minutes:D2}:{u.Seconds:D2}";
            }
        }
    }
}
