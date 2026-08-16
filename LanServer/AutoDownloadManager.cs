using System.Collections.Concurrent;

namespace LanServer
{
    /// <summary>
    /// Tracks files registered for auto-download via short LAN links.
    /// Short codes are 6-character alphanumeric strings derived from the upload time + filename.
    /// </summary>
    public class AutoDownloadEntry
    {
        public string ShortCode  { get; set; } = "";
        public string FileName   { get; set; } = "";
        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }

    public static class AutoDownloadManager
    {
        private static readonly ConcurrentDictionary<string, AutoDownloadEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

        public static event Action? EntriesChanged;

        /// <summary>Register a file for auto-download. Returns the generated short code.</summary>
        public static string Register(string fileName)
        {
            // Deterministic but unique: use ticks + filename hash → 6 uppercase alphanumeric chars
            var code = GenerateCode(fileName);
            var entry = new AutoDownloadEntry
            {
                ShortCode  = code,
                FileName   = fileName,
                UploadedAt = DateTime.Now
            };
            _entries[code] = entry;
            EntriesChanged?.Invoke();
            return code;
        }

        public static void Remove(string shortCode)
        {
            _entries.TryRemove(shortCode, out _);
            EntriesChanged?.Invoke();
        }

        public static IEnumerable<AutoDownloadEntry> GetEntries() => _entries.Values.OrderByDescending(e => e.UploadedAt);

        public static AutoDownloadEntry? GetByCode(string shortCode)
            => _entries.TryGetValue(shortCode, out var e) ? e : null;

        private static string GenerateCode(string fileName)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            // Mix ticks + filename hash for uniqueness — 4 chars gives 32^4 = ~1M combinations
            var seed = (int)(DateTime.Now.Ticks & 0x7FFFFFFF) ^ fileName.GetHashCode();
            var rng  = new Random(seed);
            var code = new char[4];
            for (int i = 0; i < 4; i++)
                code[i] = chars[rng.Next(chars.Length)];

            var candidate = new string(code);
            // Avoid collision
            while (_entries.ContainsKey(candidate))
            {
                for (int i = 0; i < 4; i++)
                    code[i] = chars[rng.Next(chars.Length)];
                candidate = new string(code);
            }
            return candidate;
        }
    }
}
